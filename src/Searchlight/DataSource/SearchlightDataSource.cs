using Searchlight.Configuration;
using Searchlight.Configuration.Default;
using Searchlight.Exceptions;
using Searchlight.Nesting;
using Searchlight.Parsing;
using Searchlight.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.DataSource
{
    /// <summary>
    /// Represents a data source used to validate queries
    /// </summary>
    public class SearchlightDataSource
    {
        /// <summary>
        /// Definitions of columns
        /// </summary>
        public ISafeColumnDefinition ColumnDefinitions { get; set; }

        /// <summary>
        /// Formatting for the output
        /// </summary>
        public IColumnify Columnifier { get; set; }
        public DataSourceType DatabaseType { get; set; }

        /// <summary>
        /// The field name of the default sort field, if none are specified.
        /// This is necessary to ensure reliable pagination.
        /// </summary>
        public string DefaultSortField { get; set; }

        /// <summary>
        /// This function produces a list of optional commands that can be specified in the $include parameter
        /// </summary>
        public OptionalCommand[] Commands { get; set; }

        /// <summary>
        /// Some data sources can only handle a specified number of parameters.
        /// </summary>
        public int MaximumParameters { get; set; }


        /// <summary>
        /// Create a searchlight data source based on an in-memory collection
        /// </summary>
        /// <typeparam name="T">The underlying data type being queried</typeparam>
        /// <param name="source">The collection to be used as the source</param>
        /// <param name="queryAllProperties">If true, all properties on the underlying data type will be queryable</param>
        /// <returns></returns>
        public static SearchlightDataSource FromCollection<T>(IEnumerable<T> source, bool queryAllProperties = true)
        {
            SearchlightDataSource src = new SearchlightDataSource();
            src.ColumnDefinitions = new EntityColumnDefinitions(typeof(T));
            src.Columnifier = new NoColumnify();
            src.DatabaseType = DataSourceType.GenericCollection;
            return src;
        }

        /// <summary>
        /// Parse a query and return only the validated information.  If any illegal values or text
        /// was provided, this function will throw a SearchlightException.
        /// </summary>
        /// <param name="include"></param>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public QueryData Parse(string include, string filter, string orderBy)
        {
            QueryData query = new QueryData();
            query.OriginalFilter = filter;
            query.Includes = ParseIncludes(include);
            query.Filter = ParseFilter(filter);
            query.OrderBy = ParseOrderBy(orderBy);
            return query;
        }

        /// <summary>
        /// Parse the include statements
        /// </summary>
        /// <param name="includes"></param>
        /// <param name="source"></param>
        public List<OptionalCommand> ParseIncludes(string includes)
        {
            // Retrieve the list of possibilities
            List<OptionalCommand> list = new List<OptionalCommand>();
            if (Commands != null) {
                list.AddRange(Commands);
            }

            // First check the field are from valid entity fields
            if (!String.IsNullOrWhiteSpace(includes))
            {
                string[] commandNames = includes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in commandNames.Select(x => x.Trim()))
                {

                    // Check to see if this is a recognized subtable that is allowed
                    bool found_command = false;
                    foreach (var command in list)
                    {
                        if (command.IsNameMatch(name))
                        {
                            command.IsIncluded = true;
                            found_command = true;
                            break;
                        }
                    }

                    // This is not recognized - throw an exception and refuse to process further
                    if (!found_command)
                    {
                        throw new FieldNameException(name, includes);
                    }
                }
            }

            // Here is the list of tested and validated commands
            return list;
        }

        /// <summary>
        /// Parses the orderBy clause requested, or if null, uses the default to ensure
        /// that pagination works
        /// </summary>
        /// <param name="orderBy"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public List<SortInfo> ParseOrderBy(string orderBy)
        {
            List<SortInfo> list = new List<SortInfo>();

            // Shortcut for case where user gives us an empty string
            if (String.IsNullOrWhiteSpace(orderBy))
            {
                list.Add(new SortInfo()
                {
                    Direction = SortDirection.Ascending,
                    Column = ColumnDefinitions.IdentifyColumn(DefaultSortField)
                });
                return list;
            }

            // Okay, let's tokenize the orderBy statement and begin parsing
            var tokens = Tokenizer.GenerateTokens(orderBy);
            SortInfo si = null;
            while (tokens.Count > 0)
            {
                si = new SortInfo();
                si.Direction = SortDirection.Ascending;
                list.Add(si);

                // Identify the field being sorted
                var colName = tokens.Dequeue();
                si.Column = ColumnDefinitions.IdentifyColumn(colName);
                if (si.Column == null)
                {
                    throw new FieldNameException(colName, orderBy);
                }

                // Was that the last token?
                if (tokens.Count == 0) break;

                // Next, we allow ASC, DESC, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.Dequeue();
                if (token == StringConstants.COMMA)
                {
                    if (tokens.Count == 0) throw new TrailingConjunctionException(orderBy);
                    continue;
                }

                // Allow ASC or DESC
                var tokenUpper = token.ToUpperInvariant();
                if (tokenUpper == StringConstants.ASCENDING)
                {
                    si.Direction = SortDirection.Ascending;
                }
                else if (tokenUpper == StringConstants.DESCENDING)
                {
                    si.Direction = SortDirection.Descending;
                }

                // Are we at the end?
                if (tokens.Count == 0) break;

                // Otherwise, we must next have a comma
                Expect(StringConstants.COMMA, tokens.Dequeue(), orderBy);
            }

            // Here's your sort info
            return list;
        }

        /// <summary>
        /// Parse the $filter parameter and turn it into a list of validated, whitelisted clauses that can 
        /// then be parsed into SQL or a LINQ statement
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<BaseClause> ParseFilter(string filter)
        {
            // Shortcut for no filter
            if (string.IsNullOrEmpty(filter))
            {
                return new List<BaseClause>();
            }

            // First parse the incoming filter into tokens
            Queue<string> tokens = Tokenizer.GenerateTokens(filter);

            // Parse a sequene of tokens
            return ParseClauseList(filter, tokens, false);
        }

        /// <summary>
        /// Parse a list of tokens separated by conjunctions
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tokens"></param>
        /// <param name="expectCloseParenthesis"></param>
        /// <returns></returns>
        private List<BaseClause> ParseClauseList(string filter, Queue<string> tokens, bool expectCloseParenthesis)
        {
            var working = new List<BaseClause>();
            while (tokens.Count > 0)
            {

                // Identify one clause and add it
                var clause = ParseOneClause(filter, tokens);
                working.Add(clause);

                // Is this the end of the filter?
                if (tokens.Count == 0) break;

                // Let's see what the next token is
                var token = tokens.Dequeue();

                // Do we end on a close parenthesis?
                if (expectCloseParenthesis && token == StringConstants.CLOSE_PARENTHESIS)
                {
                    return working;
                }

                // If not, we must have a conjunction
                string upperToken = token.ToUpperInvariant();
                string conjunction;
                if (!StringConstants.SAFE_CONJUNCTIONS.TryGetValue(upperToken, out conjunction))
                {
                    throw new ExpectedConjunctionException(token, filter);
                }

                // Store the value of the conjunction
                if (String.Equals(StringConstants.AND, upperToken))
                {
                    clause.Conjunction = ConjunctionType.AND;
                }
                else if (String.Equals(StringConstants.OR, upperToken))
                {
                    clause.Conjunction = ConjunctionType.OR;
                }
                else
                {
                    throw new UnknownConjunctionException(filter, upperToken);
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.Count == 0)
                {
                    throw new TrailingConjunctionException(filter);
                }
            }

            // If we expected to end with a parenthesis, but didn't, throw an exception here
            if (expectCloseParenthesis)
            {
                throw new OpenClauseException(filter);
            }

            // Here's your clause!
            return working;
        }

        /// <summary>
        /// Parse one single clause
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tokens"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private BaseClause ParseOneClause(string filter, Queue<string> tokens)
        {
            // First token is allowed to be a parenthesis or a field name
            string fieldToken = tokens.Dequeue();

            // Is it a parenthesis?  If so, parse a compound clause list
            if (fieldToken == StringConstants.OPEN_PARENTHESIS)
            {
                var compound = new CompoundClause();
                compound.Children = ParseClauseList(filter, tokens, true);
                if (compound.Children == null || compound.Children.Count == 0)
                {
                    throw new EmptyClauseException(filter);
                }
                return compound;
            }

            // Identify the fieldname -- is it on the approved list?
            var columnInfo = ColumnDefinitions.IdentifyColumn(fieldToken);
            if (columnInfo == null)
            {
                throw new FieldNameException(fieldToken, filter);
            }

            // Allow "NOT" tokens here
            bool negated = false;
            var operationToken = tokens.Dequeue().ToUpperInvariant();
            if (operationToken == StringConstants.NOT)
            {
                negated = true;
                operationToken = tokens.Dequeue().ToUpperInvariant();
            }

            // Next is the operation; must validate it against our list of safe tokens.  Case insensitive.
            OperationType op = OperationType.Unknown;
            if (!StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.TryGetValue(operationToken, out op))
            {
                throw new ParserSyntaxException(operationToken, StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys, filter);
            }

            // Safe syntax for a "BETWEEN" expression is "column BETWEEN (param1) AND (param2)"
            if (op == OperationType.Between)
            {
                BetweenClause c = new BetweenClause();
                c.Negated = negated;
                c.Column = columnInfo;
                c.LowerValue = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                Expect(StringConstants.AND, tokens.Dequeue(), filter);
                c.UpperValue = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                return c;

                // Safe syntax for an "IN" expression is "column IN (param[, param][, param]...)"
            }
            else if (op == OperationType.In)
            {
                InClause c = new InClause();
                c.Column = columnInfo;
                c.Negated = negated;
                c.Values = new List<object>();
                Expect(StringConstants.OPEN_PARENTHESIS, tokens.Dequeue(), filter);
                while (true)
                {
                    c.Values.Add(ParseParameter(columnInfo, tokens.Dequeue(), filter));
                    string comma_or_paren = tokens.Dequeue();
                    if (!StringConstants.SAFE_LIST_TOKENS.Contains(comma_or_paren))
                    {
                        throw new ParserSyntaxException(comma_or_paren, StringConstants.SAFE_LIST_TOKENS, filter);
                    }
                    if (comma_or_paren == StringConstants.CLOSE_PARENTHESIS) break;
                }
                return c;

                // Safe syntax for an "IS NULL" expression is "column IS [NOT] NULL"
            }
            else if (op == OperationType.IsNull)
            {
                IsNullClause c = new IsNullClause();
                c.Column = columnInfo;

                // Allow "not" to come either before or after the "IS"
                string next = tokens.Dequeue().ToUpperInvariant();
                if (next == StringConstants.NOT)
                {
                    negated = true;
                    next = tokens.Dequeue();
                }
                c.Negated = negated;
                Expect(StringConstants.NULL, next, filter);
                return c;

                // Safe syntax for all other recognized expressions is "column op param"
            }
            else
            {
                CriteriaClause c = new CriteriaClause();
                c.Negated = negated;
                c.Operation = op;
                c.Column = columnInfo;
                c.Value = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                return c;
            }
        }

        /// <summary>
        /// Verify that the next token is an expected token
        /// </summary>
        /// <param name="expected_token"></param>
        /// <param name="actual"></param>
        /// <param name="originalFilter"></param>
        private static void Expect(string expected_token, string actual, string originalFilter)
        {
            if (!String.Equals(expected_token, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new ParserSyntaxException(actual, new string[] { expected_token }, originalFilter);
            }
        }

        /// <summary>
        /// Parse one value out of a token
        /// </summary>
        /// <param name="column"></param>
        /// <param name="valueToken"></param>
        /// <param name="originalFilter"></param>
        private static object ParseParameter(ColumnInfo column, string valueToken, string originalFilter)
        {
            // Attempt to cast this item to the specified type
            var fieldType = column.FieldType;
            var enumType = column.EnumType;
            object pvalue;
            try
            {

                // For nullable types, note that the fieldvaluetoken will always be non-null.
                // This is because the safe parser will throw an exception if there is no token after a query expression.
                // The only way to test against null is to use the special query expression "<field> IS NULL" or "<field> IS NOT NULL".
                // The proper way to unroll this is to reconsider the field type as the first generic argument to the nullable object
                if (Nullable.GetUnderlyingType(fieldType) != null)
                {
                    fieldType = column.FieldType.GetGenericArguments()[0];
                }

                // If this is an object that must be parsed as an enum, permit the user to specify the enum as a string
                if (enumType != null)
                {
                    if (Nullable.GetUnderlyingType(enumType) != null)
                    {
                        enumType = enumType.GetGenericArguments()[0];
                    }
                    var o = Enum.Parse(enumType, valueToken);
                    pvalue = Convert.ChangeType(o, fieldType);

                    // Guid parsing
                }
                else if (fieldType == typeof(Guid))
                {
                    pvalue = Guid.Parse(valueToken);

                    // Special handling for UINT64 to handle certain database servers
                }
                else if (fieldType == typeof(UInt64))
                {
                    bool boolVal;
                    if (bool.TryParse(valueToken, out boolVal))
                    {
                        pvalue = boolVal ? 1UL : 0;
                    }
                    else
                    {
                        pvalue = Convert.ChangeType(valueToken, fieldType);
                    }

                    // All others use the default behavior
                }
                else
                {
                    pvalue = Convert.ChangeType(valueToken, fieldType);
                }

                // Value could not be converted to the specified type
            }
            catch
            {
                throw new FieldValueException(column.FieldName, fieldType.ToString(), valueToken, originalFilter);
            }

            // Put this into an SQL Parameter list
            return pvalue;
        }

    }
}
