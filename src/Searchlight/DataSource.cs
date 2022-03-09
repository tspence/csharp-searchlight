using Searchlight.Nesting;
using Searchlight.Parsing;
using Searchlight.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Searchlight.Exceptions;

namespace Searchlight
{
    /// <summary>
    /// Represents a data source used to validate queries
    /// </summary>
    public class DataSource
    {
        /// <summary>
        /// The engine to use for related tables
        /// </summary>
        public SearchlightEngine Engine { get; set; }

        /// <summary>
        /// The externally visible name of this collection or table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The internal type of the model queried by this source
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        /// The field name of the default sort field, if none are specified.
        /// This is necessary to ensure reliable pagination.
        /// </summary>
        public string DefaultSort { get; set; }

        /// <summary>
        /// This function produces a list of optional commands that can be specified in the $include parameter
        /// </summary>
        public List<ICommand> Commands { get; set; }

        /// <summary>
        /// The list of flags that can be specified in the $include parameter
        /// </summary>
        public List<SearchlightFlag> Flags { get; set; }

        /// <summary>
        /// Some data sources can only handle a specified number of parameters.
        /// If set at the data source level, this overrides the value set on the SearchlightEngine object.
        /// </summary>
        public int? MaximumParameters { get; set; }

        private readonly List<string> _knownIncludes = new List<string>();
        private readonly Dictionary<string, object> _includeDict = new Dictionary<string, object>();
        private readonly Dictionary<string, ColumnInfo> _fieldDict = new Dictionary<string, ColumnInfo>();
        private readonly List<ColumnInfo> _columns = new List<ColumnInfo>();

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public DataSource WithColumn(string columnName, Type columnType)
        {
            return WithRenamingColumn(columnName, columnName, null, columnType);
        }

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="filterName"></param>
        /// <param name="columnName"></param>
        /// <param name="aliases"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public DataSource WithRenamingColumn(string filterName, string columnName, string[] aliases, Type columnType)
        {
            var columnInfo = new ColumnInfo(filterName, columnName, aliases, columnType);
            _columns.Add(columnInfo);

            // Allow the API caller to either specify either the model name or one of the aliases
            AddName(filterName, columnInfo);
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    AddName(alias, columnInfo);
                }
            }

            return this;
        }

        private void AddName(string name, ColumnInfo col)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var upperName = name.Trim().ToUpperInvariant();
            if (_fieldDict.ContainsKey(upperName))
            {
                var existing = _fieldDict[upperName];
                throw new DuplicateName
                {
                    Table = this.TableName,
                    ExistingColumn = existing.FieldName,
                    ConflictingColumn = col.FieldName,
                    ConflictingName = upperName,
                };
            }

            _fieldDict[upperName] = col;
        }

        private void AddInclude(string name, object incl)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var upperName = name.Trim().ToUpperInvariant();
            if (_includeDict.ContainsKey(upperName))
            {
                throw new DuplicateInclude
                {
                    Table = this.TableName,
                    ConflictingIncludeName = upperName
                };
            }

            _includeDict[upperName] = incl;
        }

        /// <summary>
        /// Gets the list of columns for this data source
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ColumnInfo> GetColumnDefinitions()
        {
            return _columns;
        }

        /// <summary>
        /// Gets the list of column names for this data source
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ColumnNames()
        {
            return _fieldDict.Keys;
        }

        /// <summary>
        /// Identify a single column by its token
        /// </summary>
        /// <param name="filterToken"></param>
        /// <returns></returns>
        public ColumnInfo IdentifyColumn(string filterToken)
        {
            if (string.IsNullOrWhiteSpace(filterToken)) return null;
            _fieldDict.TryGetValue(filterToken.ToUpper(), out ColumnInfo ci);
            return ci;
        }


        /// <summary>
        /// Create a searchlight data source based on an in-memory collection
        /// </summary>
        /// <param name="engine">The engine containing all child tables for this data source; null if this is a standalone table</param>
        /// <param name="modelType">The type of the model for this data source</param>
        /// <param name="mode">The parsing mode for fields on this class</param>
        /// <returns></returns>
        public static DataSource Create(SearchlightEngine engine, Type modelType, AttributeMode mode)
        {
            var src = new DataSource
            {
                Engine = engine,
                Commands = new List<ICommand>(),
                Flags = modelType.GetCustomAttributes<SearchlightFlag>().ToList()
            };
            var modelAttribute = modelType.GetCustomAttribute<SearchlightModel>();
            src.ModelType = modelType;
            if (modelAttribute == null && mode == AttributeMode.Strict)
            {
                throw new NonSearchlightModel { ModelTypeName = modelType.Name };
            }
            if (modelAttribute != null)
            {
                src.TableName = modelAttribute.OriginalName ?? modelType.Name;
                src.MaximumParameters = modelAttribute.MaximumParameters;
                src.DefaultSort = modelAttribute.DefaultSort;
            }
            else
            {
                src.TableName = modelType.Name;
            }
            foreach (var pi in modelType.GetProperties())
            {
                // Searchlight does not support list/array element syntax
                if (pi.GetIndexParameters().Length == 0)
                {
                    if (mode == AttributeMode.Loose)
                    {
                        src.WithColumn(pi.Name, pi.PropertyType);
                    }
                    else
                    {
                        var filter = pi.GetCustomAttributes<SearchlightField>().FirstOrDefault();
                        if (filter != null)
                        {
                            // If this is a renaming column, add it appropriately
                            Type t = filter.FieldType ?? pi.PropertyType;
                            src.WithRenamingColumn(pi.Name, filter.OriginalName ?? pi.Name,
                                filter.Aliases ?? Array.Empty<string>(), t);
                        }

                        var collection = pi.GetCustomAttributes<SearchlightCollection>().FirstOrDefault();
                        if (collection != null)
                        {
                            src.Commands.Add(new CollectionCommand(src, collection, pi));
                        }
                    }
                }
            }
            
            // default sort cannot be null and must be a valid column
            if (src.DefaultSort != null)
            {
                try
                {
                    var sort = src.ParseOrderBy(src.DefaultSort);
                    if (sort.Count == 0)
                    {
                        throw new InvalidDefaultSort
                            {Table = src.TableName, DefaultSort = src.DefaultSort};
                    }
                }
                catch
                {
                    throw new InvalidDefaultSort
                        {Table = src.TableName, DefaultSort = src.DefaultSort};
                }
            }
            else
            {
                throw new InvalidDefaultSort
                    {Table = src.TableName, DefaultSort = "NULL"};
            }

            // Calculate the list of known "include" commands
            foreach (var cmd in src.Commands)
            {
                src.AddInclude(cmd.GetName(), cmd);
                src._knownIncludes.Add(cmd.GetName());
                foreach (var name in cmd.GetAliases())
                {
                    src.AddInclude(name, cmd);
                }
            }
            foreach (var flag in src.Flags)
            {
                src.AddInclude(flag.Name, flag);
                src._knownIncludes.Add(flag.Name);
                if (flag.Aliases != null)
                {
                    foreach (var alias in flag.Aliases)
                    {
                        src.AddInclude(alias, flag);
                    }
                }
            }

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
        public SyntaxTree Parse(string filter, string include = null, string orderBy = null)
        {
            var fetch = new FetchRequest { filter = filter, include = include, order = orderBy };
            return Parse(fetch);
        }

        /// <summary>
        /// Parse a fetch request object into a syntax tree
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidPageSize"></exception>
        /// <exception cref="InvalidPageNumber"></exception>
        public SyntaxTree Parse(FetchRequest request)
        {
            var query = new SyntaxTree
            {
                Source = this,
                OriginalFilter = request.filter,
            };
            
            var tuple = ParseIncludes(request.include);
            query.Includes = tuple.Item1;
            query.Flags = tuple.Item2;
            query.Filter = ParseFilter(request.filter);
            query.OrderBy = ParseOrderBy(request.order);
            if (request.pageNumber != null || request.pageSize != null)
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
        /// <param name="includes">The names of collections to fetch</param>
        public Tuple<List<ICommand>, List<SearchlightFlag>> ParseIncludes(string includes)
        {
            // We will collect results here
            var list = new List<ICommand>();
            var flags = new List<SearchlightFlag>();
            if (!string.IsNullOrWhiteSpace(includes))
            {
                foreach (var n in includes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = n.Trim();
                    if (name != null)
                    {
                        var upperName = name.Trim()?.ToUpperInvariant();
                        if (_includeDict.TryGetValue(upperName, out var obj))
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
                                KnownIncludes = _knownIncludes.ToArray()
                            };
                        }
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
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public List<SortInfo> ParseOrderBy(string orderBy)
        {
            List<SortInfo> list = new List<SortInfo>();
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = DefaultSort;
            }

            // If no sort is specified
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return list;
            }

            // Okay, let's tokenize the orderBy statement and begin parsing
            var tokens = Tokenizer.GenerateTokens(orderBy);
            while (tokens.Count > 0)
            {
                var si = new SortInfo { Direction = SortDirection.Ascending };
                list.Add(si);

                // Identify the field being sorted
                var colName = tokens.Dequeue();
                si.Column = IdentifyColumn(colName);
                if (si.Column == null)
                {
                    throw new FieldNotFound()
                    {
                        FieldName = colName, KnownFields = ColumnNames().ToArray(), OriginalFilter = orderBy
                    };
                }

                // Was that the last token?
                if (tokens.Count == 0) break;

                // Next, we allow ASC or ASCENDING, DESC or DESCENDING, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.Dequeue();
                if (token == StringConstants.COMMA)
                {
                    if (tokens.Count == 0) throw new TrailingConjunction() { OriginalFilter = orderBy };
                    continue;
                }

                // Allow ASC or DESC
                var tokenUpper = token.ToUpperInvariant();
                if (tokenUpper == StringConstants.ASCENDING || 
                    tokenUpper == StringConstants.ASCENDING_ABR)
                {
                    si.Direction = SortDirection.Ascending;
                }
                else if (tokenUpper == StringConstants.DESCENDING || 
                         tokenUpper == StringConstants.DESCENDING_ABR)
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

            // Parse a sequence of tokens
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
                if (!StringConstants.SAFE_CONJUNCTIONS.ContainsKey(upperToken))
                {
                    throw new InvalidToken { BadToken = upperToken, ExpectedTokens = StringConstants.SAFE_CONJUNCTIONS.Keys.ToArray(), OriginalFilter = filter};
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
                    throw new InvalidToken { BadToken = upperToken, ExpectedTokens = new[] { "AND", "OR" }, OriginalFilter = filter };
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.Count == 0)
                {
                    throw new TrailingConjunction() { OriginalFilter = filter };
                }
            }

            // If we expected to end with a parenthesis, but didn't, throw an exception here
            if (expectCloseParenthesis)
            {
                throw new OpenClause { OriginalFilter = filter };
            }

            // Here's your clause!
            return working;
        }

        /// <summary>
        /// Parse one single clause
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private BaseClause ParseOneClause(string filter, Queue<string> tokens)
        {
            // First token is allowed to be a parenthesis or a field name
            string fieldToken = tokens.Dequeue();

            // Is it a parenthesis?  If so, parse a compound clause list
            if (fieldToken == StringConstants.OPEN_PARENTHESIS)
            {
                var compound = new CompoundClause { Children = ParseClauseList(filter, tokens, true) };
                if (compound.Children == null || compound.Children.Count == 0)
                {
                    throw new EmptyClause() { OriginalFilter = filter};
                }

                return compound;
            }

            // Identify the fieldname -- is it on the approved list?
            var columnInfo = IdentifyColumn(fieldToken);
            if (columnInfo == null)
            {
                if (string.Equals(fieldToken, StringConstants.CLOSE_PARENTHESIS))
                {
                    throw new EmptyClause() { OriginalFilter = filter};
                }

                throw new FieldNotFound() { FieldName = fieldToken, KnownFields = ColumnNames().ToArray(), OriginalFilter = filter };
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
            if (!StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.TryGetValue(operationToken, out OperationType op))
            {
                throw new InvalidToken()
                {
                    BadToken = operationToken,
                    ExpectedTokens = StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys.ToArray(),
                    OriginalFilter = filter
                };
            }

            switch (op)
            {
                // Safe syntax for a "BETWEEN" expression is "column BETWEEN (param1) AND (param2)"
                case OperationType.Between:
                    BetweenClause b = new BetweenClause
                    {
                        Negated = negated,
                        Column = columnInfo,
                        LowerValue = ParseParameter(columnInfo, tokens.Dequeue(), filter)
                    };
                    Expect(StringConstants.AND, tokens.Dequeue(), filter);
                    b.UpperValue = ParseParameter(columnInfo, tokens.Dequeue(), filter);
                    return b;

                // Safe syntax for an "IN" expression is "column IN (param[, param][, param]...)"
                case OperationType.In:
                    InClause i = new InClause
                    {
                        Column = columnInfo,
                        Negated = negated,
                        Values = new List<object>()
                    };
                    Expect(StringConstants.OPEN_PARENTHESIS, tokens.Dequeue(), filter);

                    if (tokens.Peek() != StringConstants.CLOSE_PARENTHESIS)
                    {
                        while (true)
                        {
                            i.Values.Add(ParseParameter(columnInfo, tokens.Dequeue(), filter));
                            string commaOrParen = tokens.Dequeue();
                            if (!StringConstants.SAFE_LIST_TOKENS.Contains(commaOrParen))
                            {
                                throw new InvalidToken { BadToken = commaOrParen, ExpectedTokens = StringConstants.SAFE_LIST_TOKENS, OriginalFilter = filter };
                            }

                            if (commaOrParen == StringConstants.CLOSE_PARENTHESIS) break;
                        }
                    }
                    else
                    {
                        throw new EmptyClause { OriginalFilter = filter };
                    }

                    return i;

                // Safe syntax for an "IS NULL" expression is "column IS [NOT] NULL"
                case OperationType.IsNull:
                    IsNullClause iN = new IsNullClause { Column = columnInfo };

                    // Allow "not" to come either before or after the "IS"
                    string next = tokens.Dequeue().ToUpperInvariant();
                    if (next == StringConstants.NOT)
                    {
                        negated = true;
                        next = tokens.Dequeue();
                    }

                    iN.Negated = negated;
                    Expect(StringConstants.NULL, next, filter);
                    return iN;

                // Safe syntax for all other recognized expressions is "column op param"
                default:
                    CriteriaClause c = new CriteriaClause
                    {
                        Negated = negated,
                        Operation = op,
                        Column = columnInfo,
                        Value = ParseParameter(columnInfo, tokens.Dequeue(), filter)
                    };

                    if (c.Operation == OperationType.StartsWith || c.Operation == OperationType.EndsWith
                                                                || c.Operation == OperationType.Contains)
                    {
                        if (c.Column.FieldType != typeof(string))
                        {
                            throw new FieldTypeMismatch() { 
                                FieldName = c.Column.FieldName, 
                                FieldType = c.Column.FieldType.ToString(), 
                                FieldValue = Convert.ToString(c.Value), 
                                OriginalFilter = filter
                            };
                        }
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

        private static object DefinedDateOperators(string valueToken)
        {
            StringConstants.DEFINED_DATES.TryGetValue(valueToken.ToUpper(), out var result);
            return (result != null) ? (object)result.Invoke() : valueToken;
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

                if (fieldType == typeof(Guid))
                {
                    pvalue = Guid.Parse(valueToken);

                    // Special handling for UINT64 to handle certain database servers
                }
                else if (fieldType == typeof(UInt64))
                {
                    if (bool.TryParse(valueToken, out var boolVal))
                    {
                        pvalue = boolVal ? 1UL : 0;
                    }
                    else
                    {
                        pvalue = Convert.ChangeType(valueToken, fieldType);
                    }

                    // All others use the default behavior
                }
                else if (fieldType == typeof(DateTime))
                {
                    var definedDate = DefinedDateOperators(valueToken);
                    // if date is same as the original, convert to DateTime, else make pvalue defined date
                    pvalue = definedDate.ToString() == valueToken ? Convert.ChangeType(valueToken, fieldType) : definedDate;
                }
                else
                {
                    pvalue = Convert.ChangeType(valueToken, fieldType);
                }

                // Value could not be converted to the specified type
            }
            catch
            {
                throw new FieldTypeMismatch {
                    FieldName = column.FieldName, 
                    FieldType = fieldType.ToString(), 
                    FieldValue = valueToken, 
                    OriginalFilter = originalFilter
                };
            }

            // Put this into an SQL Parameter list
            return pvalue;
        }
    }
}