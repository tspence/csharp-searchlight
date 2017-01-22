using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Searchlight.Configuration;
using Searchlight.Query;
using Dapper;
using System.Reflection;
using Searchlight.Nesting;
using Searchlight.Exceptions;

namespace Searchlight.Parsing
{
    /// <summary>
    /// Filter parser class that analyzes a filter against a reflected object to validate the criteria
    /// </summary>
    public class SafeQueryParser
    {
        private readonly ISafeColumnDefinition _safeColumnDefinition;
        private readonly IColumnify _columnifier;
        private readonly DatabaseType _databaseType;

        public SafeQueryParser(ISafeColumnDefinition safeColumnDefinition, IColumnify columnifier, DatabaseType databaseType)
        {
            _safeColumnDefinition = safeColumnDefinition;
            _columnifier = columnifier;
            _databaseType = databaseType;
        }

        public SelectClause ParseSelectClause(string fieldsToLoad, IEnumerable<OptionalCommand> commands = null)
        {
            SelectClause clause = new SelectClause();
            clause.SelectFieldList = new List<string>();
            clause.SubtableList = new List<string>();

            // First check the field are from valid entity fields
            if (!string.IsNullOrEmpty(fieldsToLoad) && !fieldsToLoad.Equals("*")) {
                string[] fieldNames = fieldsToLoad.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var fieldName in fieldNames.Select(x => x.Trim())) {
                    if (fieldName == "*") continue;
                    var col = _safeColumnDefinition.IdentifyColumn(fieldName);

                    // Was this field recognized as a valid field in the table?
                    if (col != null) {
                        clause.SelectFieldList.Add(_columnifier.Columnify(col.DatabaseColumn, ClauseType.Select));
                    } else {

                        // Check to see if this is a recognized subtable that is allowed
                        bool found_command = false;
                        if (commands != null) {

                            // Find a match!
                            foreach (var command in commands) {
                                if (command.IsNameMatch(fieldName)) {
                                    command.IsIncluded = true;
                                    found_command = true;
                                    break;
                                }
                            }
                        }

                        // This is not recognized - throw an exception and refuse to process further
                        if (!found_command) {
                            throw new FieldNameException(fieldName, fieldsToLoad);
                        }
                    }
                }
            }

            // List all fields
            if (clause.SelectFieldList.Count == 0) {
                foreach (var column in _safeColumnDefinition.GetColumnDefinitions()) {
                    clause.SelectFieldList.Add(_columnifier.Columnify(column.DatabaseColumn, ClauseType.Select));
                }
            }

            return clause;
        }

        /// <summary>
        /// Parses the orderBy clause requested, or if null, uses the default
        /// </summary>
        /// <param name="orderBy"></param>
        /// <param name="defaultOrderBy"></param>
        /// <returns></returns>
        public OrderByClause ParseOrderByClause(string orderBy, string defaultOrderBy)
        {
            // Shortcut for case where user gives us an empty string
            if (String.IsNullOrWhiteSpace(orderBy)) {
                orderBy = defaultOrderBy;
            }

            // Okay, let's tokenize the user's input
            OrderByClause orderByClause = new OrderByClause();
            orderByClause.SortInfoList = Tokenizer.TokenizeOrderBy(orderBy);

            // Now, go through each token and parse it into a valid column
            StringBuilder sql = new StringBuilder();
            foreach (var order in orderByClause.SortInfoList) {
                var col = _safeColumnDefinition.IdentifyColumn(order.Fieldname);
                if (col == null) {
                    throw new FieldNameException(order.Fieldname, "");
                }
                sql.Append(_columnifier.Columnify(col.DatabaseColumn, ClauseType.OrderBy));
                sql.Append((order.Direction == SortDirection.Ascending ? " ASC" : " DESC"));
                sql.Append(", ");
            }

            // There is always at least one item in this list, so remove the last comma
            sql.Length -= 2;

            // Here's your order by clause
            orderByClause.Expression = sql.ToString();
            return orderByClause;
        }

        /// <summary>
        /// Parse this "WHERE" clause and only allow specific whitelisted query expressions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public WhereClause ParseWhereClause(string filter)
        {
            // Shortcut for no filter
            WorkingResults working = new WorkingResults();
            if (string.IsNullOrEmpty(filter)) {
                return working.ToSafeQuery();
            }

            // First parse the incoming filter into tokens, then start iterating over them
            bool is_started = false;
            Queue<string> tokens = Tokenizer.GenerateTokens(filter);
            int clauseDepth = 0;
            bool anyActualCriteria = false;
            while (tokens.Count > 0) {

                // Consume one token - this is the field
                string fieldToken = tokens.Dequeue();

                // Allow the user to put as many connectors in as they like.  Connectors can't cause the query to be sql-injection-vulnerable.
                bool any_conjunctions = false;
                while (true) {

                    // Find this match; if no more conjunctions, exit
                    var upperToken = fieldToken.ToUpperInvariant();
                    string insertToken = null;
                    if (!StringConstants.SAFE_CONJUNCTIONS.TryGetValue(upperToken, out insertToken)) {
                        break;
                    }

                    // Count number of open or closed parenthesis
                    if (fieldToken == StringConstants.OPEN_PARENTHESIS) {
                        clauseDepth++;
                    } else if (fieldToken == StringConstants.CLOSE_PARENTHESIS) {
                        clauseDepth--;
                    }

                    // We had at least one conjunction, insert it
                    any_conjunctions = true;
                    working.FilterSql.Append(insertToken);

                    // Is that the end of our filter?
                    if (tokens.Count == 0) {

                        // If the user attempted to end on an "OR" clause without continuing, throw that error
                        if (!StringConstants.SAFE_ENDING_CONJUNCTIONS.Contains(fieldToken)) {
                            throw new TrailingConjunctionException(filter);
                        }

                        // It's okay to end the query here
                        fieldToken = null;
                        break;
                    }

                    // Next token!
                    fieldToken = tokens.Dequeue();
                }

                // If we're done
                if (fieldToken == null) break;

                // If we are in the middle of the filter, and there were no conjunctions between fields, throw an error
                if (!any_conjunctions && is_started) {
                    throw new ParserSyntaxException(fieldToken, StringConstants.SAFE_CONJUNCTIONS.Keys, filter);
                }
                is_started = true;

                // Identify the fieldname -- is it on the approved list?
                var columnInfo = _safeColumnDefinition.IdentifyColumn(fieldToken);
                if (columnInfo == null) {
                    throw new FieldNameException(fieldToken, filter);
                }

                // Append the column to our result
                working.FilterSql.Append(_columnifier.Columnify(columnInfo.DatabaseColumn, ClauseType.Where));
                anyActualCriteria = true;

                // Allow "NOT" tokens here
                string user_operation_token = tokens.Dequeue().ToUpper();
                if (user_operation_token == "NOT") {
                    working.FilterSql.Append(" ");
                    working.FilterSql.Append(user_operation_token);
                    working.FilterSql.Append(" ");
                    user_operation_token = tokens.Dequeue().ToUpper();
                }

                // Next is the operation; must validate it against our list of safe tokens.  Case insensitive.
                string sql_operation_token = null;
                if (!StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.TryGetValue(user_operation_token, out sql_operation_token)) {
                    throw new ParserSyntaxException(user_operation_token, StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys, filter);
                }
                working.FilterSql.Append(sql_operation_token);

                // Safe syntax for a "BETWEEN" expression is "BETWEEN (param1) AND (param2)"
                if (sql_operation_token == " BETWEEN ") {
                    AddParameter(columnInfo.DatabaseColumn, tokens.Dequeue(), columnInfo.FieldType, columnInfo.EnumType, filter, working);
                    Expect("AND", tokens.Dequeue(), filter);
                    working.FilterSql.Append(" AND ");
                    AddParameter(columnInfo.DatabaseColumn, tokens.Dequeue(), columnInfo.FieldType, columnInfo.EnumType, filter, working);

                // Safe syntax for an "IN" expression is "IN (param[, param][, param]...)"
                } else if (sql_operation_token == " IN ") {
                    Expect("(", tokens.Dequeue(), filter);
                    working.FilterSql.Append("(");
                    while (true) {
                        AddParameter(columnInfo.DatabaseColumn, tokens.Dequeue(), columnInfo.FieldType, columnInfo.EnumType, filter, working);
                        string comma_or_paren = tokens.Dequeue();
                        if (!StringConstants.SAFE_LIST_TOKENS.Contains(comma_or_paren)) {
                            throw new ParserSyntaxException(comma_or_paren, StringConstants.SAFE_LIST_TOKENS, filter);
                        }
                        if (comma_or_paren == ")") break;
                        working.FilterSql.Append(", ");
                    }
                    working.FilterSql.Append(")");

                // Safe syntax for an "IS NULL" expression is "IS [NOT] NULL"
                } else if (sql_operation_token == " IS ") {
                    string next = tokens.Dequeue();

                    if (string.Equals(next, "NOT", StringComparison.OrdinalIgnoreCase)) {
                        working.FilterSql.Append("NOT ");
                        next = tokens.Dequeue();
                    }

                    Expect("NULL", next, filter);
                    working.FilterSql.Append("NULL");

                // Safe syntax for all other recognized expressions is "op param"
                } else {

                    // Convert synthetic "like" commands into real "like" commands
                    var val = tokens.Dequeue();
                    if (user_operation_token == "STARTSWITH") {
                        val = val + "%";
                    } else if (user_operation_token == "CONTAINS") {
                        val = "%" + val + "%";
                    } else if (user_operation_token == "ENDSWITH") {
                        val = "%" + val;
                    }

                    // Go to normal operations
                    AddParameter(columnInfo.DatabaseColumn, val, columnInfo.FieldType, columnInfo.EnumType, filter, working);
                }
            }

            // If that last one wasn't a safe ending, throw an exception!
            if (clauseDepth != 0) {
                throw new OpenClauseException(filter);
            }

            // If the user never actually sent any filter criteria, that's a different exception
            if (!anyActualCriteria) {
                throw new NoFilterCriteriaException(filter);
            }

            // Here's your safely parsed query
            return working.ToSafeQuery();
        }

        private static void Expect(string expected_token, string actual, string originalFilter)
        {
            if (!String.Equals(expected_token, actual, StringComparison.OrdinalIgnoreCase)) {
                throw new ParserSyntaxException(actual, new string[] { expected_token }, originalFilter);
            }
        }

        private void AddParameter(string fieldName, string fieldValueToken, Type fieldType, Type enumType, string originalFilter, WorkingResults workingResults)
        {
            // SQL Server statements have a limit of 2000 parameters per query
            if (_databaseType == DatabaseType.SqlServer && workingResults.NumParameters > 1999) {
                throw new TooManyParametersException(originalFilter);
            }

            // Figure out what parameter number we are
            string pname = "@p" + workingResults.NumParameters;
            workingResults.NumParameters++;
            workingResults.FilterSql.Append(pname);

            // Attempt to cast this item to the specified type
            object pvalue;
            try {

                // For nullable types, note that the fieldvaluetoken will always be non-null.
                // This is because the safe parser will throw an exception if there is no token after a query expression.
                // The only way to test against null is to use the special query expression "<field> IS NULL" or "<field> IS NOT NULL".
                // The proper way to unroll this is to reconsider the field type as the first generic argument to the nullable object
                if (Nullable.GetUnderlyingType(fieldType) != null) {
                    fieldType = fieldType.GetGenericArguments()[0];
                }

                // If this is an object that must be parsed as an enum, permit the user to specify the enum as a string
                if (enumType != null) {
                    if (Nullable.GetUnderlyingType(enumType) != null) {
                        enumType = enumType.GetGenericArguments()[0];
                    }
                    var o = Enum.Parse(enumType, fieldValueToken);
                    pvalue = Convert.ChangeType(o, fieldType);

                // Guid parsing
                } else if (fieldType == typeof(Guid)) {
                    pvalue = Guid.Parse(fieldValueToken);

                // Special handling for UINT64 to handle certain database servers
                } else if (fieldType == typeof(UInt64)) {
                    bool boolVal;
                    if (bool.TryParse(fieldValueToken, out boolVal)) {
                        pvalue = boolVal ? 1UL : 0;
                    } else {
                        pvalue = Convert.ChangeType(fieldValueToken, fieldType);
                    }

                // All others use the default behavior
                } else {
                    pvalue = Convert.ChangeType(fieldValueToken, fieldType);
                }

            // Value could not be converted to the specified type
            } catch {
                throw new FieldValueException(fieldName, fieldType.ToString(), fieldValueToken, originalFilter);
            }

            // Put this into an SQL Parameter list
            workingResults.SqlParameters.Add(pname, pvalue);
        }

        private class WorkingResults
        {
            public readonly StringBuilder FilterSql = new StringBuilder();
            public readonly DynamicParameters SqlParameters = new DynamicParameters();
            public int NumParameters = 1; // why start at 1? Seems like working around some off by 1 error somewhere

            public WhereClause ToSafeQuery()
            {
                return new WhereClause
                {
                    SqlParameters = SqlParameters,
                    ValidatedFilter = FilterSql.ToString()
                };
            }
        }
    }
}
