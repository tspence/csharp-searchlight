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
using Searchlight.DataSource;

namespace Searchlight.Parsing
{
    /// <summary>
    /// Filter parser class that analyzes a filter against a reflected object to validate the criteria
    /// </summary>
    public class SafeQueryParser
    {
        #region Interface
        /// <summary>
        /// Parse a query and return only the validated information.  If any illegal values or text
        /// was provided, this function will throw a SearchlightException.
        /// </summary>
        /// <param name="include"></param>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public static QueryData Parse(string include, string filter, string orderBy, SearchlightDataSource source)
        {
            QueryData query = new QueryData();
            query.Includes = ParseIncludes(include, source);
            query.Filter = ParseFilter(filter, source);
            query.OrderBy = ParseOrderBy(orderBy, source);
            return query;
        }

        /// <summary>
        /// Parse the include statements
        /// </summary>
        /// <param name="includes"></param>
        /// <param name="source"></param>
        public static List<OptionalCommand> ParseIncludes(string includes, SearchlightDataSource source)
        {
            // Retrieve the list of possibilities
            List<OptionalCommand> list = new List<OptionalCommand>();
            var raw = source.Commands();
            if (raw != null) {
                list.AddRange(raw);
            }

            // First check the field are from valid entity fields
            if (!String.IsNullOrWhiteSpace(includes)) {
                string[] commandNames = includes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in commandNames.Select(x => x.Trim())) {

                    // Check to see if this is a recognized subtable that is allowed
                    bool found_command = false;
                    foreach (var command in list) {
                        if (command.IsNameMatch(name)) {
                            command.IsIncluded = true;
                            found_command = true;
                            break;
                        }
                    }

                    // This is not recognized - throw an exception and refuse to process further
                    if (!found_command) {
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
        public static List<SortInfo> ParseOrderBy(string orderBy, SearchlightDataSource source)
        {
            List<SortInfo> list = new List<SortInfo>();

            // Shortcut for case where user gives us an empty string
            if (String.IsNullOrWhiteSpace(orderBy)) {
                list.Add(new SortInfo() {
                    Direction = SortDirection.Ascending,
                    Column = source.ColumnDefinitions.IdentifyColumn(source.DefaultSortField)
                });
                return list;
            }

            // Okay, let's tokenize the orderBy statement and begin parsing
            var tokens = Tokenizer.GenerateTokens(orderBy);
            SortInfo si = null;
            while (tokens.Count > 0) {
                si = new SortInfo();
                si.Direction = SortDirection.Ascending;
                list.Add(si);

                // Identify the field being sorted
                var colName = tokens.Dequeue();
                var col = source.ColumnDefinitions.IdentifyColumn(colName);
                if (col == null) {
                    throw new FieldNameException(colName, orderBy);
                }

                // Was that the last token?
                if (tokens.Count == 0) break;

                // Next, we allow ASC, DESC, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.Dequeue();
                if (token == StringConstants.COMMA) continue;

                // Allow ASC or DESC
                var tokenUpper = token.ToUpperInvariant();
                if (tokenUpper == StringConstants.ASCENDING) {
                    si.Direction = SortDirection.Ascending;
                } else if (tokenUpper == StringConstants.DESCENDING) {
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
        public static List<BaseClause> ParseFilter(string filter, SearchlightDataSource source)
        {
            // Shortcut for no filter
            if (string.IsNullOrEmpty(filter)) {
                return new List<BaseClause>();
            }

            // First parse the incoming filter into tokens
            Queue<string> tokens = Tokenizer.GenerateTokens(filter);

            // Parse a sequene of tokens
            return ParseClauseList(filter, tokens, source, false);
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Parse a list of tokens separated by conjunctions
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tokens"></param>
        /// <param name="source"></param>
        /// <param name="expectCloseParenthesis"></param>
        /// <returns></returns>
        private static List<BaseClause> ParseClauseList(string filter, Queue<string> tokens, SearchlightDataSource source, bool expectCloseParenthesis)
        {
            var working = new List<BaseClause>();
            while (tokens.Count > 0) {

                // Identify one clause and add it
                var clause = ParseOneClause(filter, tokens, source);
                working.Add(clause);

                // Is this the end of the filter?
                if (tokens.Count == 0) break;

                // Let's see what the next token is
                var token = tokens.Dequeue();

                // Do we end on a close parenthesis?
                if (expectCloseParenthesis && token == StringConstants.CLOSE_PARENTHESIS) {
                    return working;
                }

                // If not, we must have a conjunction
                string conjunction;
                if (!StringConstants.SAFE_CONJUNCTIONS.TryGetValue(token.ToUpperInvariant(), out conjunction)) {
                    throw new ExpectedConjunctionException(token, filter);
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.Count == 0) {
                    throw new TrailingConjunctionException(filter);
                }
            }

            // If we expected to end with a parenthesis, but didn't, throw an exception here
            if (expectCloseParenthesis) {
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
        private static BaseClause ParseOneClause(string filter, Queue<string> tokens, SearchlightDataSource source)
        {
            // First token is allowed to be a parenthesis or a field name
            string fieldToken = tokens.Dequeue();

            // Is it a parenthesis?  If so, parse a compound clause list
            if (fieldToken == StringConstants.OPEN_PARENTHESIS) {
                var compound = new CompoundClause();
                compound.Children = ParseClauseList(filter, tokens, source, true);
                if (compound.Children == null || compound.Children.Count == 0) {
                    throw new EmptyClauseException(filter);
                }
                return compound;
            }

            // Identify the fieldname -- is it on the approved list?
            var columnInfo = source.ColumnDefinitions.IdentifyColumn(fieldToken);
            if (columnInfo == null) {
                throw new FieldNameException(fieldToken, filter);
            }

            // Allow "NOT" tokens here
            bool negated = false;
            var operationToken = tokens.Dequeue().ToUpperInvariant();
            if (operationToken == StringConstants.NOT) {
                negated = true;
                operationToken = tokens.Dequeue().ToUpperInvariant();
            }

            // Next is the operation; must validate it against our list of safe tokens.  Case insensitive.
            OperationType op = OperationType.Unknown;
            if (!StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.TryGetValue(operationToken, out op)) {
                throw new ParserSyntaxException(operationToken, StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys, filter);
            }

            // Safe syntax for a "BETWEEN" expression is "column BETWEEN (param1) AND (param2)"
            if (op == OperationType.Between) {
                BetweenClause c = new BetweenClause();
                c.Negated = negated;
                c.Column = columnInfo;
                c.LowerValue = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                Expect(StringConstants.AND, tokens.Dequeue(), filter);
                c.UpperValue = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                return c;

            // Safe syntax for an "IN" expression is "column IN (param[, param][, param]...)"
            } else if (op == OperationType.In) {
                InClause c = new InClause();
                c.Column = columnInfo;
                c.Negated = negated;
                c.Values = new List<object>();
                Expect(StringConstants.OPEN_PARENTHESIS, tokens.Dequeue(), filter);
                while (true) {
                    c.Values.Add(ParseParameter(columnInfo, tokens.Dequeue(), filter));
                    string comma_or_paren = tokens.Dequeue();
                    if (!StringConstants.SAFE_LIST_TOKENS.Contains(comma_or_paren)) {
                        throw new ParserSyntaxException(comma_or_paren, StringConstants.SAFE_LIST_TOKENS, filter);
                    }
                    if (comma_or_paren == StringConstants.CLOSE_PARENTHESIS) break;
                }
                return c;

            // Safe syntax for an "IS NULL" expression is "column IS [NOT] NULL"
            } else if (op == OperationType.IsNull) {
                IsNullClause c = new IsNullClause();
                c.Column = columnInfo;

                // Allow "not" to come either before or after the "IS"
                string next = tokens.Dequeue().ToUpperInvariant();
                if (next == StringConstants.NOT) {
                    negated = true;
                    next = tokens.Dequeue();
                }
                c.Negated = negated;
                Expect(StringConstants.NULL, next, filter);
                return c;

            // Safe syntax for all other recognized expressions is "column op param"
            } else {
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
            if (!String.Equals(expected_token, actual, StringComparison.OrdinalIgnoreCase)) {
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
            try {

                // For nullable types, note that the fieldvaluetoken will always be non-null.
                // This is because the safe parser will throw an exception if there is no token after a query expression.
                // The only way to test against null is to use the special query expression "<field> IS NULL" or "<field> IS NOT NULL".
                // The proper way to unroll this is to reconsider the field type as the first generic argument to the nullable object
                if (Nullable.GetUnderlyingType(fieldType) != null) {
                    fieldType = column.FieldType.GetGenericArguments()[0];
                }

                // If this is an object that must be parsed as an enum, permit the user to specify the enum as a string
                if (enumType != null) {
                    if (Nullable.GetUnderlyingType(enumType) != null) {
                        enumType = enumType.GetGenericArguments()[0];
                    }
                    var o = Enum.Parse(enumType, valueToken);
                    pvalue = Convert.ChangeType(o, fieldType);

                // Guid parsing
                } else if (fieldType == typeof(Guid)) {
                    pvalue = Guid.Parse(valueToken);

                // Special handling for UINT64 to handle certain database servers
                } else if (fieldType == typeof(UInt64)) {
                    bool boolVal;
                    if (bool.TryParse(valueToken, out boolVal)) {
                        pvalue = boolVal ? 1UL : 0;
                    } else {
                        pvalue = Convert.ChangeType(valueToken, fieldType);
                    }

                // All others use the default behavior
                } else {
                    pvalue = Convert.ChangeType(valueToken, fieldType);
                }

            // Value could not be converted to the specified type
            } catch {
                throw new FieldValueException(column.FieldName, fieldType.ToString(), valueToken, originalFilter);
            }

            // Put this into an SQL Parameter list
            return pvalue;
        }
        #endregion
    }
}
