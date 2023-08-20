using System;
using System.Collections.Generic;
using System.Linq;
using Searchlight.Exceptions;
using Searchlight.Expressions;
using Searchlight.Nesting;
using Searchlight.Query;

namespace Searchlight.Parsing
{
    public class SyntaxParser
    {
        /// <summary>
        /// Parse a query and return only the validated information.  If any illegal values or text
        /// was provided, this function will throw a SearchlightException.
        /// </summary>
        public static SyntaxTree Parse(DataSource source, string filter, string include = null, string orderBy = null)
        {
            var fetch = new FetchRequest { filter = filter, include = include, order = orderBy };
            return Parse(source, fetch);
        }

        /// <summary>
        /// Parse a fetch request object into a syntax tree
        /// </summary>
        public static SyntaxTree Parse(DataSource source, FetchRequest request)
        {
            var query = new SyntaxTree
            {
                Source = source,
                OriginalFilter = request?.filter,
            };
            
            var tuple = ParseIncludes(source, request?.include);
            query.Includes = tuple.Item1;
            query.Flags = tuple.Item2;
            query.Filter = ParseFilter(source, request?.filter);
            query.OrderBy = ParseOrderBy(source, request?.order);
            if (request?.pageNumber != null || request?.pageSize != null)
            {
                query.PageNumber = request.pageNumber ?? 0;
                query.PageSize = request.pageSize ?? 50;
                if (query.PageSize <= 0)
                {
                    throw new InvalidPageSize { PageSize = request.pageSize == null ? "not specified" : request.pageSize.ToString() };
                }

                if (query.PageNumber < 0)
                {
                    throw new InvalidPageNumber { PageNumber = request.pageNumber == null ? "not specified" : request.pageNumber.ToString() };
                }
            }

            return query;
        }

        /// <summary>
        /// Specify the name of optional collections or commands to include in this fetch request separated by commas.
        /// </summary>
        private static Tuple<List<ICommand>, List<SearchlightFlag>> ParseIncludes(DataSource source, string includes)
        {
            // We will collect results here
            var list = new List<ICommand>();
            var flags = new List<SearchlightFlag>();
            if (!string.IsNullOrWhiteSpace(includes))
            {
                foreach (var n in includes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = n.Trim();
                    var upperName = name.Trim().ToUpperInvariant();
                    if (source._includeDict.TryGetValue(upperName, out var obj))
                    {
                        if (obj is ICommand command)
                        {
                            list.Add(command);
                        }
                        else if (obj is SearchlightFlag flag)
                        {
                            flags.Add(flag);
                        }
                    }
                    else
                    {
                        throw new IncludeNotFound()
                        {
                            OriginalInclude = includes,
                            IncludeName = name,
                            KnownIncludes = source._knownIncludes.ToArray()
                        };
                    }
                }
            }

            // Here is the list of tested and validated commands
            return new Tuple<List<ICommand>, List<SearchlightFlag>>(list, flags);
        }

        /// <summary>
        /// Parses the orderBy clause requested, or if null, uses the default to ensure
        /// that pagination works
        /// </summary>
        public static List<SortInfo> ParseOrderBy(DataSource source, string orderBy)
        {
            var list = new List<SortInfo>();
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = source.DefaultSort;
            }

            // If no sort is specified
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return list;
            }

            // Okay, let's tokenize the orderBy statement and begin parsing
            var tokens = Tokenizer.GenerateTokens(orderBy);
            while (tokens.TokenQueue.Count > 0)
            {
                var si = new SortInfo { Direction = SortDirection.Ascending };
                list.Add(si);

                // Identify the field being sorted
                var colName = tokens.TokenQueue.Dequeue();
                si.Column = source.IdentifyColumn(colName.Value);
                if (si.Column == null)
                {
                    throw new FieldNotFound()
                    {
                        FieldName = colName.Value, KnownFields = source.ColumnNames().ToArray(), OriginalFilter = orderBy
                    };
                }

                // Was that the last token?
                if (tokens.TokenQueue.Count == 0) break;

                // Next, we allow ASC or ASCENDING, DESC or DESCENDING, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.TokenQueue.Dequeue();
                if (token.Value == StringConstants.COMMA)
                {
                    if (tokens.TokenQueue.Count == 0) throw new TrailingConjunction() { OriginalFilter = orderBy };
                    continue;
                }

                // Allow ASC or DESC
                var tokenUpper = token.Value.ToUpperInvariant();
                if (tokenUpper == StringConstants.ASCENDING || 
                    tokenUpper == StringConstants.ASCENDING_ABBR)
                {
                    si.Direction = SortDirection.Ascending;
                }
                else if (tokenUpper == StringConstants.DESCENDING || 
                         tokenUpper == StringConstants.DESCENDING_ABBR)
                {
                    si.Direction = SortDirection.Descending;
                }

                // Are we at the end?
                if (tokens.TokenQueue.Count == 0) break;

                // Otherwise, we must next have a comma
                Expect(StringConstants.COMMA, tokens.TokenQueue.Dequeue().Value, orderBy);
            }

            // Here's your sort info
            return list;
        }

        /// <summary>
        /// Parse the $filter parameter and turn it into a list of validated clauses that can 
        /// then be rendered as SQL or a LINQ expression
        /// </summary>
        public static List<BaseClause> ParseFilter(DataSource source, string filter)
        {
            // Shortcut for no filter
            if (string.IsNullOrEmpty(filter))
            {
                return new List<BaseClause>();
            }

            // First parse the incoming filter into tokens
            var tokens = Tokenizer.GenerateTokens(filter);
            if (tokens.HasUnterminatedLiteral)
            {
                throw new UnterminatedString()
                {
                    OriginalFilter = filter,
                    StartPosition = tokens.LastStringLiteralBegin 
                };
            }

            // Parse a sequence of tokens
            return ParseClauseList(source, tokens, false);
        }

        /// <summary>
        /// Parse a list of tokens separated by conjunctions
        /// </summary>
        private static List<BaseClause> ParseClauseList(DataSource source, TokenStream tokens, bool expectCloseParenthesis)
        {
            var working = new List<BaseClause>();
            while (tokens.TokenQueue.Count > 0)
            {
                // Identify one clause and add it
                var clause = ParseOneClause(source, tokens);
                working.Add(clause);

                // Is this the end of the filter?
                if (tokens.TokenQueue.Count == 0) break;

                // Let's see what the next token is
                var token = tokens.TokenQueue.Dequeue();

                // Do we end on a close parenthesis?
                if (expectCloseParenthesis && token.Value == StringConstants.CLOSE_PARENTHESIS)
                {
                    return CheckConjunctions(working);
                }

                // If not, we must have a conjunction
                string upperToken = token.Value.ToUpperInvariant();
                if (!StringConstants.SAFE_CONJUNCTIONS.ContainsKey(upperToken))
                {
                    throw new InvalidToken { BadToken = upperToken, ExpectedTokens = StringConstants.SAFE_CONJUNCTIONS.Keys.ToArray(), OriginalFilter = tokens.OriginalText};
                }

                // Store the value of the conjunction
                if (string.Equals(StringConstants.AND, upperToken))
                {
                    clause.Conjunction = ConjunctionType.AND;
                }
                else if (string.Equals(StringConstants.OR, upperToken))
                {
                    clause.Conjunction = ConjunctionType.OR;
                }
                else
                {
                    throw new InvalidToken { BadToken = upperToken, ExpectedTokens = new[] { "AND", "OR" }, OriginalFilter = tokens.OriginalText };
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.TokenQueue.Count == 0)
                {
                    throw new TrailingConjunction() { OriginalFilter = tokens.OriginalText };
                }
            }

            // If we expected to end with a parenthesis, but didn't, throw an exception here
            if (expectCloseParenthesis)
            {
                throw new OpenClause { OriginalFilter = tokens.OriginalText };
            }

            // Let's verify that the clause is fully valid first before accepting it
            return CheckConjunctions(working);
        }

        private static List<BaseClause> CheckConjunctions(List<BaseClause> clauses)
        {
            var conjunctions = (from item in clauses where item.Conjunction != ConjunctionType.NONE select item.Conjunction)
                .Distinct();
            if (conjunctions.Count() > 1)
            {
                throw new InconsistentConjunctionException()
                {
                    InconsistentClause = string.Join(" ", from item in clauses select item + " " + (item.Conjunction == ConjunctionType.NONE ? string.Empty : item.Conjunction.ToString())).TrimEnd(),
                };
            }

            return clauses;
        }

        /// <summary>
        /// Parse one single clause
        /// </summary>
        private static BaseClause ParseOneClause(DataSource source, TokenStream tokens)
        {
            // First token is allowed to be a parenthesis or a field name
            var fieldToken = tokens.TokenQueue.Dequeue();

            // Is it a parenthesis?  If so, parse a compound clause list
            if (fieldToken.Value == StringConstants.OPEN_PARENTHESIS)
            {
                var compound = new CompoundClause { Children = ParseClauseList(source, tokens, true) };
                if (compound.Children == null || compound.Children.Count == 0)
                {
                    throw new EmptyClause() { OriginalFilter = tokens.OriginalText };
                }

                return compound;
            }

            // Identify the field name -- is it on the approved list?
            var columnInfo = source.IdentifyColumn(fieldToken.Value);
            if (columnInfo == null)
            {
                if (string.Equals(fieldToken.Value, StringConstants.CLOSE_PARENTHESIS))
                {
                    throw new EmptyClause() { OriginalFilter = tokens.OriginalText };
                }

                throw new FieldNotFound() { FieldName = fieldToken.Value, KnownFields = source.ColumnNames().ToArray(), OriginalFilter = tokens.OriginalText };
            }

            // Allow "NOT" tokens here
            var negated = false;
            var operationToken = tokens.TokenQueue.Dequeue().Value.ToUpperInvariant();
            if (operationToken == StringConstants.NOT)
            {
                negated = true;
                operationToken = tokens.TokenQueue.Dequeue().Value.ToUpperInvariant();
            }

            // Next is the operation; must validate it against our list of safe tokens.  Case insensitive.
            if (!StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.TryGetValue(operationToken, out var op))
            {
                throw new InvalidToken()
                {
                    BadToken = operationToken,
                    ExpectedTokens = StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys.ToArray(),
                    OriginalFilter = tokens.OriginalText
                };
            }

            switch (op)
            {
                // Safe syntax for a "BETWEEN" expression is "column BETWEEN (param1) AND (param2)"
                case OperationType.Between:
                    var b = new BetweenClause
                    {
                        Negated = negated,
                        Column = columnInfo,
                        LowerValue = ParseParameter(columnInfo, tokens.TokenQueue.Dequeue().Value, tokens)
                    };
                    Expect(StringConstants.AND, tokens.TokenQueue.Dequeue().Value, tokens.OriginalText);
                    b.UpperValue = ParseParameter(columnInfo, tokens.TokenQueue.Dequeue().Value, tokens);
                    return b;

                // Safe syntax for an "IN" expression is "column IN (param[, param][, param]...)"
                case OperationType.In:
                    var i = new InClause
                    {
                        Column = columnInfo,
                        Negated = negated,
                        Values = new List<IExpressionValue>()
                    };
                    Expect(StringConstants.OPEN_PARENTHESIS, tokens.TokenQueue.Dequeue().Value, tokens.OriginalText);

                    if (tokens.TokenQueue.Peek().Value != StringConstants.CLOSE_PARENTHESIS)
                    {
                        while (true)
                        {
                            i.Values.Add(ParseParameter(columnInfo, tokens.TokenQueue.Dequeue().Value, tokens));
                            var commaOrParen = tokens.TokenQueue.Dequeue();
                            if (!StringConstants.SAFE_LIST_TOKENS.Contains(commaOrParen.Value))
                            {
                                throw new InvalidToken { BadToken = commaOrParen.Value, ExpectedTokens = StringConstants.SAFE_LIST_TOKENS, OriginalFilter = tokens.OriginalText };
                            }

                            if (commaOrParen.Value == StringConstants.CLOSE_PARENTHESIS) break;
                        }
                    }
                    else
                    {
                        throw new EmptyClause { OriginalFilter = tokens.OriginalText };
                    }

                    return i;

                // Safe syntax for an "IS NULL" expression is "column IS [NOT] NULL"
                case OperationType.IsNull:
                    var iN = new IsNullClause { Column = columnInfo };

                    // Allow "not" to come either before or after the "IS"
                    var next = tokens.TokenQueue.Dequeue().Value.ToUpperInvariant();
                    if (next == StringConstants.NOT)
                    {
                        negated = true;
                        next = tokens.TokenQueue.Dequeue().Value;
                    }

                    iN.Negated = negated;
                    Expect(StringConstants.NULL, next, tokens.OriginalText);
                    return iN;

                // Safe syntax for all other recognized expressions is "column op param"
                default:
                    var valueToken = tokens.TokenQueue.Dequeue();
                    var c = new CriteriaClause
                    {
                        Negated = negated,
                        Operation = op,
                        Column = columnInfo,
                        Value = ParseParameter(columnInfo, valueToken.Value, tokens)
                    };

                    if ((c.Operation == OperationType.StartsWith || c.Operation == OperationType.EndsWith
                                                                 || c.Operation == OperationType.Contains) &&
                        (c.Column.FieldType != typeof(string)))
                    {
                        throw new FieldTypeMismatch()
                        {
                            FieldName = c.Column.FieldName,
                            FieldType = c.Column.FieldType.ToString(),
                            FieldValue = valueToken.Value,
                            OriginalFilter = tokens.OriginalText
                        };
                    }

                    return c;
            }
        }

        /// <summary>
        /// Verify that the next token is an expected token
        /// </summary>
        /// <param name="expectedToken"></param>
        /// <param name="actual"></param>
        /// <param name="originalFilter"></param>
        private static void Expect(string expectedToken, string actual, string originalFilter)
        {
            if (!string.Equals(expectedToken, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidToken() { BadToken = actual, ExpectedTokens = new[] { expectedToken }, OriginalFilter = originalFilter };
            }
        }

        /// <summary>
        /// Parse one value out of a token
        /// </summary>
        /// <param name="column"></param>
        /// <param name="valueToken"></param>
        /// <param name="tokens"></param>
        private static IExpressionValue ParseParameter(ColumnInfo column, string valueToken, TokenStream tokens)
        {
            var fieldType = column.FieldType;
            try
            {
                // For nullable types, note that the field value token will always be non-null.
                // This is because the safe parser will throw an exception if there is no token after a query expression.
                // The only way to test against null is to use the special query expression "<field> IS NULL" or "<field> IS NOT NULL".
                // The proper way to unroll this is to reconsider the field type as the first generic argument to the nullable object
                if (Nullable.GetUnderlyingType(fieldType) != null)
                {
                    fieldType = column.FieldType.GetGenericArguments()[0];
                }

                if (fieldType == typeof(Guid))
                {
                    return ConstantValue.From(Guid.Parse(valueToken));
                }
                
                // Special handling for UINT64 to handle certain database servers
                if (fieldType == typeof(UInt64))
                {
                    if (bool.TryParse(valueToken, out var boolVal))
                    {
                        return ConstantValue.From(boolVal ? 1UL : 0);
                    }
                }

                // DateTime objects can use computational math
                if (fieldType == typeof(DateTime))
                {
                    var tokenUpper = valueToken.ToUpper();
                    
                    // Is this a defined date, potentially with math?
                    if (StringConstants.DEFINED_DATES.ContainsKey(tokenUpper))
                    {
                        var computedValue = new ComputedDateValue()
                        {
                            Root = tokenUpper,
                        };
                        var nextToken = tokens.TokenQueue.Count > 0 ? tokens.TokenQueue.Peek() : null;
                        if (nextToken != null && (nextToken.Value == StringConstants.ADD || nextToken.Value == StringConstants.SUBTRACT))
                        {
                            // Retrieve the direction and offset
                            var direction = tokens.TokenQueue.Dequeue();
                            var offset = tokens.TokenQueue.Dequeue();
                            var ok = int.TryParse(offset.Value, out var offsetValue);
                            if (!ok)
                            {
                                throw new InvalidToken()
                                {
                                    BadToken = offset.Value,
                                    ExpectedTokens = new [] { "an integer" },
                                };
                            }

                            // Handle negative offsets
                            if (direction.Value == StringConstants.SUBTRACT)
                            {
                                offsetValue = -offsetValue;
                            }

                            computedValue.Offset = offsetValue;
                        }
                        return computedValue;
                    }
                }

                // All other types use a basic type changer
                return ConstantValue.From(Convert.ChangeType(valueToken, fieldType));
            }
            catch
            {
                throw new FieldTypeMismatch {
                    FieldName = column.FieldName, 
                    FieldType = fieldType.ToString(), 
                    FieldValue = valueToken, 
                    OriginalFilter = tokens.OriginalText
                };
            }
        }

    }
}