﻿using Searchlight.Nesting;
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
        /// Some data sources can only handle a specified number of parameters.
        /// </summary>
        public int MaximumParameters { get; set; }

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
            var upperName = name.ToUpper();
            if (_fieldDict.ContainsKey(upperName))
            {
                var existing = _fieldDict[upperName];
                throw new DuplicateName
                {
                    ExistingColumn = existing.OriginalName, ConflictingColumn = col.OriginalName,
                    ConflictingName = upperName
                };
            }

            _fieldDict[upperName] = col;
        }

        public IEnumerable<ColumnInfo> GetColumnDefinitions()
        {
            return _columns;
        }

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
            if (String.IsNullOrWhiteSpace(filterToken)) return null;
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
            var src = new DataSource();
            src.Engine = engine;
            src.Commands = new List<ICommand>();
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
                                filter.Aliases ?? new string[] { }, t);
                        }

                        var collection = pi.GetCustomAttributes<SearchlightCollection>().FirstOrDefault();
                        if (collection != null)
                        {
                            src.Commands.Add(new CollectionCommand(src, collection, pi));
                        }
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
            var fetch = new FetchRequest {filter = filter, include = include, order = orderBy};
            return Parse(fetch);
        }

        public SyntaxTree Parse(FetchRequest request)
        {
            SyntaxTree query = new SyntaxTree
            {
                Source = this, OriginalFilter = request.filter, Includes = ParseIncludes(request.include)
            };
            
            foreach (var cmd in query.Includes)
            {
                cmd.Preview(request);
            }
            query.Filter = ParseFilter(request.filter);
            query.OrderBy = ParseOrderBy(request.order);
            if (request.pageNumber != null || request.pageSize != null)
            {
                query.PageNumber = request.pageNumber ?? 0;
                query.PageSize = request.pageSize ?? 50;
                if (query.PageSize <= 1)
                {
                    throw new InvalidPageSize {PageSize = request.pageSize == null ? "not specified" : request.pageSize.ToString()};
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
        public List<ICommand> ParseIncludes(string includes)
        {
            // We will collect results here
            var list = new List<ICommand>();
            if (!string.IsNullOrWhiteSpace(includes))
            {
                foreach (var name in includes.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var matchingCommand = (from command in Commands where command.MatchesName(name) select command)
                        .FirstOrDefault();
                    if (matchingCommand == null)
                    {
                        throw new FieldNotFound(name, ColumnNames().ToArray(), includes);
                    }
                    list.Add(matchingCommand);
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
        /// <returns></returns>
        public List<SortInfo> ParseOrderBy(string orderBy)
        {
            List<SortInfo> list = new List<SortInfo>();
            if (String.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = DefaultSort;
            }

            // If no sort is specified
            if (String.IsNullOrWhiteSpace(orderBy))
            {
                return list;
            }

            // Okay, let's tokenize the orderBy statement and begin parsing
            var tokens = Tokenizer.GenerateTokens(orderBy);
            while (tokens.Count > 0)
            {
                var si = new SortInfo {Direction = SortDirection.Ascending};
                list.Add(si);

                // Identify the field being sorted
                var colName = tokens.Dequeue();
                si.Column = IdentifyColumn(colName);
                if (si.Column == null)
                {
                    throw new FieldNotFound(colName, ColumnNames().ToArray(), orderBy);
                }

                // Was that the last token?
                if (tokens.Count == 0) break;

                // Next, we allow ASC, DESC, or a comma (indicating another sort).
                // First, check for the case of a comma
                var token = tokens.Dequeue();
                if (token == StringConstants.COMMA)
                {
                    if (tokens.Count == 0) throw new TrailingConjunction(orderBy);
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
                if (!StringConstants.SAFE_CONJUNCTIONS.TryGetValue(upperToken, out string conjunction))
                {
                    throw new InvalidToken(upperToken, StringConstants.SAFE_CONJUNCTIONS.Keys.ToArray(), filter);
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
                    throw new InvalidToken(upperToken, new [] {"AND", "OR"}, filter);
                }

                // Is this the end of the filter?  If so that's a trailing conjunction error
                if (tokens.Count == 0)
                {
                    throw new TrailingConjunction(filter);
                }
            }

            // If we expected to end with a parenthesis, but didn't, throw an exception here
            if (expectCloseParenthesis)
            {
                throw new OpenClause(filter);
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
                var compound = new CompoundClause {Children = ParseClauseList(filter, tokens, true)};
                if (compound.Children == null || compound.Children.Count == 0)
                {
                    throw new EmptyClause(filter);
                }

                return compound;
            }

            // Identify the fieldname -- is it on the approved list?
            var columnInfo = IdentifyColumn(fieldToken);
            if (columnInfo == null)
            {
                if (String.Equals(fieldToken, StringConstants.CLOSE_PARENTHESIS))
                {
                    throw new EmptyClause(filter);
                }

                throw new FieldNotFound(fieldToken, ColumnNames().ToArray(), filter);
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
                throw new InvalidToken(operationToken, StringConstants.RECOGNIZED_QUERY_EXPRESSIONS.Keys.ToArray(),
                    filter);
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
                        Column = columnInfo, Negated = negated, Values = new List<object>()
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
                                throw new InvalidToken(commaOrParen, StringConstants.SAFE_LIST_TOKENS, filter);
                            }

                            if (commaOrParen == StringConstants.CLOSE_PARENTHESIS) break;
                        }
                    }
                    else
                    {
                        throw new EmptyClause(filter);
                    }

                    return i;
                
                // Safe syntax for an "IS NULL" expression is "column IS [NOT] NULL"
                case OperationType.IsNull:
                    IsNullClause iN = new IsNullClause {Column = columnInfo};

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
                            throw new FieldTypeMismatch(c.Column.FieldName, c.Column.FieldType.ToString(), Convert.ToString(c.Value), filter);
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
            if (!String.Equals(expectedToken, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidToken(actual, new [] {expectedToken}, originalFilter);
            }
        }

        private static object DefinedDateOperators(string valueToken)
        {
            if (StringConstants.DefinedDates.Keys.Contains(valueToken, StringComparer.OrdinalIgnoreCase))
            {
                StringConstants.DefinedDates.TryGetValue(valueToken.ToUpper(), out var result);
                if (result != null) return result.Invoke();
            }

            return valueToken;
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
                throw new FieldTypeMismatch(column.FieldName, fieldType.ToString(), valueToken, originalFilter);
            }

            // Put this into an SQL Parameter list
            return pvalue;
        }
    }
}