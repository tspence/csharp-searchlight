using System;
using System.Collections.Generic;
using System.Linq;
using Searchlight.Exceptions;
using Searchlight.Expressions;
using Searchlight.Nesting;
using Searchlight.Query;

namespace Searchlight.Parsing
{
    public static class SyntaxParser
    {
        /// <summary>
        /// Shortcut for Parse using a syntax tree.
        /// </summary>
        public static SyntaxTree Parse(DataSource source, string filter, string include, string orderBy)
        {
            var fetch = new FetchRequest { filter = filter, include = include, order = orderBy };
            return Parse(source, fetch);
        }

        /// <summary>
        /// Tries to parse a fetch request object into a syntax tree.
        ///
        /// Returns a valid syntax tree if successful; throws an exception if not.
        ///
        /// You should prefer to use TryParse if possible since it will report on multiple exceptions.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static SyntaxTree Parse(DataSource source, FetchRequest request)
        {
            var syntax = TryParse(source, request);
            if (syntax.Errors != null && syntax.Errors.Count > 0)
            {
                throw syntax.Errors[0];
            }

            return syntax;
        }
        
        /// <summary>
        /// Tries to parse a fetch request object into a syntax tree.
        ///
        /// Returns either a valid syntax tree or a list of exceptions.
        /// </summary>
        public static SyntaxTree TryParse(DataSource source, FetchRequest request)
        {
            var syntax = new SyntaxTree
            {
                Source = source,
                OriginalFilter = request?.filter,
            };
            
            ParseIncludes(syntax, source, request?.include);
            ParseFilter(syntax, source, request?.filter);
            ParseOrderBy(syntax, source, request?.order);
            ParsePagination(syntax, source, request?.pageNumber, request?.pageSize);
            return syntax;
        }

        private static void ParsePagination(SyntaxTree syntax, DataSource source, int? pageNumber, int? pageSize)
        {
            if (pageNumber != null || pageSize != null)
            {
                syntax.PageNumber = pageNumber ?? 0;
                syntax.PageSize = pageSize ?? 50;
                if (syntax.PageSize <= 0)
                {
                    syntax.AddError(new InvalidPageSize
                        { PageSize = pageSize == null ? "not specified" : pageSize.ToString() });
                }

                if (syntax.PageNumber < 0)
                {
                    syntax.AddError(new InvalidPageNumber
                        { PageNumber = pageNumber == null ? "not specified" : pageNumber.ToString() });
                }
            }
        }

        /// <summary>
        /// Specify the name of optional collections or commands to include in this fetch request separated by commas.
        /// </summary>
        private static void ParseIncludes(SyntaxTree syntax, DataSource source, string includes)
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
                        syntax.AddError(new IncludeNotFound()
                        {
                            OriginalInclude = includes,
                            IncludeName = name,
                            KnownIncludes = source._knownIncludes.ToArray()
                        });
                    }
                }
            }

            // Here is the list of tested and validated commands
            syntax.Flags = flags;
            syntax.Includes = list;
        }

        /// <summary>
        /// Parses the orderBy clause requested, or if null, uses the default to ensure
        /// that pagination works
        /// </summary>
        internal static void ParseOrderBy(SyntaxTree syntax, DataSource source, string orderBy)
        {
            var list = new List<SortInfo>();
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = source.DefaultSort;
            }

            // If no sort is specified
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                syntax.OrderBy = list;
                return;
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
                    syntax.AddError(new FieldNotFound()
                    {
                        FieldName = colName.Value, KnownFields = source.ColumnNames().ToArray(), OriginalFilter = orderBy
                    });
                }

                // Was that the last token?
                if (tokens.TokenQueue.Count == 0) break;

                // Next, we allow ASC or ASCENDING, DESC or DESCENDING, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.TokenQueue.Dequeue();
                if (token.Value == StringConstants.COMMA)
                {
                    if (tokens.TokenQueue.Count == 0)
                    {
                        syntax.AddError(new TrailingConjunction() { OriginalFilter = orderBy });
                    }
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
                syntax.Expect(StringConstants.COMMA, tokens.TokenQueue.Dequeue().Value, orderBy);
            }

            // Here's your sort info
            syntax.OrderBy = list;
        }

        /// <summary>
        /// Parse the $filter parameter and turn it into a list of validated clauses that can 
        /// then be rendered as SQL or a LINQ expression
        /// </summary>
        internal static void ParseFilter(SyntaxTree syntax, DataSource source, string filter)
        {
            // Shortcut for no filter
            if (string.IsNullOrEmpty(filter))
            {
                syntax.Filter = new List<BaseClause>();
                return;
            }

            // First parse the incoming filter into tokens
            var tokens = Tokenizer.GenerateTokens(filter);
            if (tokens.HasUnterminatedLiteral)
            {
                syntax.AddError(new UnterminatedString()
                {
                    OriginalFilter = filter,
                    StartPosition = tokens.LastStringLiteralBegin 
                });
                return;
            }

            // Parse a sequence of tokens
            syntax.Filter = ParseClauseList(syntax, source, tokens, false);
        }

        /// <summary>
        /// Parse a list of tokens separated by conjunctions
        /// </summary>
        private static List<BaseClause> ParseClauseList(SyntaxTree syntax, DataSource source, TokenStream tokens, bool expectCloseParenthesis)
        {
            var working = new List<BaseClause>();
            while (tokens.TokenQueue.Count > 0)
            {
                // Identify one clause and add it
                var clause = ParseOneClause(syntax, source, tokens);
                if (clause != null)
                {
                    working.Add(clause);
                }
                else
                {
                    clause = new MalformedClause();
                }

                // Is this the end of the filter?
                if (tokens.TokenQueue.Count == 0) break;

                // Let's see what the next token is
                var token = tokens.TokenQueue.Dequeue();

                // Do we end on a close parenthesis?
                if (expectCloseParenthesis && token.Value == StringConstants.CLOSE_PARENTHESIS)
                {
                    return CheckConjunctions(syntax, working);
                }

                // Search for the end of this clause
                string upperToken = token.Value.ToUpperInvariant();
                while (!syntax.Expect(StringConstants.SAFE_CONJUNCTIONS.Keys.ToArray(), upperToken, tokens.OriginalText) && tokens.TokenQueue.Count > 0)
                {
                    token = tokens.TokenQueue.Dequeue();
                    upperToken = token.Value.ToUpperInvariant();
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
                    syntax.AddError( new InvalidToken { BadToken = upperToken, ExpectedTokens = new[] { "AND", "OR" }, OriginalFilter = tokens.OriginalText });
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.TokenQueue.Count == 0)
                {
                    syntax.AddError( new TrailingConjunction() { OriginalFilter = tokens.OriginalText });
                }
            }

            // If we expected to end with a parenthesis, but didn't, report a problem
            if (expectCloseParenthesis)
            {
                syntax.AddError(new OpenClause { OriginalFilter = tokens.OriginalText });
            }

            // Let's verify that the clause is fully valid first before accepting it
            return CheckConjunctions(syntax, working);
        }

        private static List<BaseClause> CheckConjunctions(SyntaxTree syntax, List<BaseClause> clauses)
        {
            var conjunctions = (from item in clauses where item.Conjunction != ConjunctionType.NONE select item.Conjunction)
                .Distinct();
            if (conjunctions.Count() > 1)
            {
                syntax.AddError(new InconsistentConjunctionException()
                {
                    InconsistentClause = string.Join(" ", from item in clauses select item + " " + (item.Conjunction == ConjunctionType.NONE ? string.Empty : item.Conjunction.ToString())).TrimEnd(),
                });
            }

            return clauses;
        }

        /// <summary>
        /// Parse one single clause
        /// </summary>
        private static BaseClause ParseOneClause(SyntaxTree syntax, DataSource source, TokenStream tokens)
        {
            // First token is allowed to be a parenthesis or a field name
            var fieldToken = tokens.TokenQueue.Dequeue();

            // Is it a parenthesis?  If so, parse a compound clause list
            if (fieldToken.Value == StringConstants.OPEN_PARENTHESIS)
            {
                var compound = new CompoundClause { Children = ParseClauseList(syntax, source, tokens, true) };
                if (compound.Children == null || compound.Children.Count == 0)
                {
                    syntax.AddError(new EmptyClause() { OriginalFilter = tokens.OriginalText });
                }

                return compound;
            }

            // Identify the field name -- is it on the approved list?
            var columnInfo = source.IdentifyColumn(fieldToken.Value);
            if (columnInfo == null)
            {
                if (string.Equals(fieldToken.Value, StringConstants.CLOSE_PARENTHESIS))
                {
                    syntax.AddError(new EmptyClause() { OriginalFilter = tokens.OriginalText });
                }

                syntax.AddError(new FieldNotFound() { FieldName = fieldToken.Value, KnownFields = source.ColumnNames().ToArray(), OriginalFilter = tokens.OriginalText });
                return null;
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
            if (!syntax.Expect(StringConstants.RECOGNIZED_QUERY_EXPRESSIONS, operationToken, syntax.OriginalFilter,
                    out var op))
            {
                return null;
            }

            switch (op)
            {
                // Safe syntax for a "BETWEEN" expression is "column BETWEEN (param1) AND (param2)"
                case OperationType.Between:
                    var b = new BetweenClause
                    {
                        Negated = negated,
                        Column = columnInfo,
                        LowerValue = ParseParameter(syntax, columnInfo, tokens.TokenQueue.Dequeue().Value, tokens)
                    };
                    syntax.Expect(StringConstants.AND, tokens.TokenQueue.Dequeue().Value, tokens.OriginalText);
                    b.UpperValue = ParseParameter(syntax, columnInfo, tokens.TokenQueue.Dequeue().Value, tokens);
                    return b;

                // Safe syntax for an "IN" expression is "column IN (param[, param][, param]...)"
                case OperationType.In:
                    var i = new InClause
                    {
                        Column = columnInfo,
                        Negated = negated,
                        Values = new List<IExpressionValue>()
                    };
                    syntax.Expect(StringConstants.OPEN_PARENTHESIS, tokens.TokenQueue.Dequeue().Value, tokens.OriginalText);

                    if (tokens.TokenQueue.Peek().Value != StringConstants.CLOSE_PARENTHESIS)
                    {
                        while (true)
                        {
                            i.Values.Add(ParseParameter(syntax, columnInfo, tokens.TokenQueue.Dequeue().Value, tokens));
                            var commaOrParen = tokens.TokenQueue.Dequeue();
                            syntax.Expect(StringConstants.SAFE_LIST_TOKENS, commaOrParen.Value, tokens.OriginalText);
                            if (commaOrParen.Value == StringConstants.CLOSE_PARENTHESIS) break;
                        }
                    }
                    else
                    {
                        syntax.AddError(new EmptyClause { OriginalFilter = tokens.OriginalText });
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
                    syntax.Expect(StringConstants.NULL, next, tokens.OriginalText);
                    return iN;

                // Safe syntax for all other recognized expressions is "column op param"
                default:
                    var valueToken = tokens.TokenQueue.Dequeue();
                    var c = new CriteriaClause
                    {
                        Negated = negated,
                        Operation = op,
                        Column = columnInfo,
                        Value = ParseParameter(syntax, columnInfo, valueToken.Value, tokens)
                    };

                    if ((c.Operation == OperationType.StartsWith || c.Operation == OperationType.EndsWith
                                                                 || c.Operation == OperationType.Contains) &&
                        (c.Column?.FieldType != typeof(string)))
                    {
                        syntax.AddError(new FieldTypeMismatch()
                        {
                            FieldName = c.Column?.FieldName,
                            FieldType = c.Column?.FieldType?.ToString(),
                            FieldValue = valueToken.Value,
                            OriginalFilter = tokens.OriginalText
                        });
                    }

                    return c;
            }
        }

        /// <summary>
        /// Parse one value out of a token
        /// </summary>
        private static IExpressionValue ParseParameter(SyntaxTree syntax, ColumnInfo column, string valueToken, TokenStream tokens)
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
                                syntax.AddError(new InvalidToken()
                                {
                                    BadToken = offset.Value,
                                    ExpectedTokens = new [] { "an integer" },
                                });
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
                syntax.AddError(new FieldTypeMismatch {
                    FieldName = column.FieldName, 
                    FieldType = fieldType.ToString(), 
                    FieldValue = valueToken, 
                    OriginalFilter = tokens.OriginalText
                });
                return null;
            }
        }
    }
}