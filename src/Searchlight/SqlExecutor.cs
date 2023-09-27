using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Searchlight.Exceptions;
using Searchlight.Query;

namespace Searchlight
{
    /// <summary>
    /// Extension class that supports Searchlight querying on an SQL database
    /// </summary>
    public static class SqlExecutor
    {
        /// <summary>
        /// Convert this syntax tree to a query in PostgreSQL syntax
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static SqlQuery ToMySqlCommand(this SyntaxTree query)
        {
            var engine = query.Source.Engine ?? new SearchlightEngine();
            var sql = CreateSql(SqlDialect.MySql, query, engine);
            var where = sql.WhereClause.Length > 0 ? $" WHERE {sql.WhereClause}" : "";
            var order = sql.OrderByClause.Length > 0 ? $" ORDER BY {sql.OrderByClause}" : "";
            var offset = RenderOffsetClause(SqlDialect.MySql, query.PageSize, query.PageNumber, engine);

            // Apply all selected commands
            foreach (var cmd in query.Includes)
            {
                cmd.Apply(sql);
            }

            sql.CommandText = $"{DecorateIntro(SqlDialect.MySql, engine)}" +
                              $"SELECT * FROM {DecorateTableName(SqlDialect.MySql, query.Source.TableName, engine)}{where}{order}{offset}";
            return sql;
        }
        /// <summary>
        /// Convert this syntax tree to a query in PostgreSQL syntax
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static SqlQuery ToPostgresCommand(this SyntaxTree query)
        {
            var engine = query.Source.Engine ?? new SearchlightEngine();
            var sql = CreateSql(SqlDialect.PostgreSql, query, engine);
            var where = sql.WhereClause.Length > 0 ? $" WHERE {sql.WhereClause}" : "";
            var order = sql.OrderByClause.Length > 0 ? $" ORDER BY {sql.OrderByClause}" : "";
            var offset = RenderOffsetClause(SqlDialect.PostgreSql, query.PageSize, query.PageNumber, engine);

            // Apply all selected commands
            foreach (var cmd in query.Includes)
            {
                cmd.Apply(sql);
            }

            sql.CommandText = $"{DecorateIntro(SqlDialect.PostgreSql, engine)}" +
                              $"SELECT * FROM {DecorateTableName(SqlDialect.PostgreSql, query.Source.TableName, engine)}{where}{order}{offset}";
            return sql;
        }

        private static SqlQuery CreateSql(SqlDialect dialect, SyntaxTree query, SearchlightEngine engine)
        {
            var sql = new SqlQuery() { Syntax = query };
            sql.WhereClause = RenderJoinedClauses(dialect, query.Filter, sql);
            sql.OrderByClause = RenderOrderByClause(query.OrderBy);

            // Sanity test - is the query too complicated to be safe to run?
            var maxParams = query.Source.MaximumParameters ?? engine.MaximumParameters ?? 0;
            if (maxParams > 0 && sql.Parameters.Count > maxParams)
            {
                throw new TooManyParameters()
                    { MaximumParameterCount = maxParams, OriginalFilter = query.OriginalFilter };
            }

            return sql;
        }

        private static object RenderOffsetClause(SqlDialect dialect, int? queryPageSize, int? queryPageNumber,
            SearchlightEngine engine)
        {
            var limit = (queryPageSize ?? 0) == 0 ? engine.DefaultPageSize : queryPageSize.Value;
            if (limit != null)
            {
                var offset = (queryPageNumber ?? 0) * limit;
                switch (dialect)
                {
                    case SqlDialect.MySql:
                    case SqlDialect.PostgreSql:
                        return $" LIMIT {limit} OFFSET {offset}";
                    case SqlDialect.MicrosoftSqlServer:
                        return $" OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Convert this syntax tree to a query in Microsoft SQL Server T/SQL syntax
        /// </summary>
        /// <param name="query">The query to convert to SQL text</param>
        public static SqlQuery ToSqlServerCommand(this SyntaxTree query)
        {
            var engine = query.Source.Engine ?? new SearchlightEngine();
            var sql = CreateSql(SqlDialect.MicrosoftSqlServer, query, engine);
            var where = sql.WhereClause.Length > 0 ? $" WHERE {sql.WhereClause}" : "";
            var order = sql.OrderByClause.Length > 0 ? $" ORDER BY {sql.OrderByClause}" : "";
            var offset = RenderOffsetClause(SqlDialect.MicrosoftSqlServer, query.PageSize, query.PageNumber, engine);

            // Apply all selected commands
            foreach (var cmd in query.Includes)
            {
                cmd.Apply(sql);
            }

            // If the user wants multi-fetch to retrieve row count
            if (engine.useResultSet)
            {
                // If we're doing multi-fetch, we have to retrieve sorted/paginated records into a temp table before
                // joining with any child collections
                if (sql.ResultSetClauses.Count > 0)
                {
                    var commandClauses = sql.ResultSetClauses.Count > 0
                        ? string.Join("\n", sql.ResultSetClauses) + "\n"
                        : "";
                    sql.CommandText = $"{DecorateIntro(SqlDialect.MicrosoftSqlServer, engine)}" +
                                      $"SELECT COUNT(1) AS TotalRecords FROM {query.Source.TableName}{where};\n" +
                                      $"SELECT * INTO #temp FROM {query.Source.TableName}{where}{order}{offset};\n" +
                                      $"SELECT * FROM #temp{order};\n" +
                                      commandClauses +
                                      $"DROP TABLE #temp;\n";
                }
                else
                {
                    sql.CommandText = $"{DecorateIntro(SqlDialect.MicrosoftSqlServer, engine)}" +
                                      $"SELECT COUNT(1) AS TotalRecords FROM {DecorateTableName(SqlDialect.MicrosoftSqlServer, query.Source.TableName, engine)}{where};\n" +
                                      $"SELECT * FROM {DecorateTableName(SqlDialect.MicrosoftSqlServer, query.Source.TableName, engine)}{where}{order}{offset};\n";
                }
            }
            else
            {
                sql.CommandText = $"{DecorateIntro(SqlDialect.MicrosoftSqlServer, engine)}" +
                                  $"SELECT * FROM {DecorateTableName(SqlDialect.MicrosoftSqlServer, query.Source.TableName, engine)}{where}{order}{offset}";
            }

            return sql;
        }

        private static string RenderOrderByClause(List<SortInfo> list)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var sort = list[i];
                var dir = sort.Direction == SortDirection.Ascending ? "ASC" : "DESC";

                var columnName = sort.Column.OriginalName;
                if (sort.Column.IsJson)
                {
                    columnName = $"JSON_VALUE({sort.Column.OriginalName}, '$.\"{sort.Column.JsonKey}\"')";
                }
                
                sb.Append($"{columnName} {dir}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Render a list of joined clauses using specified conjunctions
        /// </summary>
        /// <param name="dialect"></param>
        /// <param name="clause"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        private static string RenderJoinedClauses(SqlDialect dialect, List<BaseClause> clause, SqlQuery sql)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < clause.Count; i++)
            {
                if (i > 0)
                {
                    switch (clause[i - 1].Conjunction)
                    {
                        case ConjunctionType.AND:
                            sb.Append(" AND ");
                            break;
                        case ConjunctionType.OR:
                            sb.Append(" OR ");
                            break;
                        case ConjunctionType.NONE:
                        default:
                            throw new NotImplementedException();
                    }
                }

                sb.Append(RenderClause(dialect, clause[i], sql));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert a single clause object into SQL-formatted "WHERE" text
        /// </summary>
        /// <param name="dialect"></param>
        /// <param name="clause"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string RenderClause(SqlDialect dialect, BaseClause clause, SqlQuery sql)
        {
            if (clause?.Column?.IsJson ?? false)
            {
                // JSON_VALUE(EmployeeObj.dims, '$."test"')
                var columnName = $"JSON_VALUE({sql.Syntax.Source.TableName}.{clause.Column.OriginalName}, '$.\"{clause.Column.JsonKey}\"')";

                switch (clause)
                {
                    case CompoundClause compoundClause:
                        return $"({RenderJoinedClauses(dialect, compoundClause.Children, sql)})";
                    case CriteriaClause cc:
                        var rawValue = cc.Value.GetValue();
                        switch (cc.Operation)
                        {
                            case OperationType.Equals:
                            case OperationType.NotEqual:
                                return RenderComparisonClause(columnName, cc.Negated, cc.Operation,
                                    sql.AddParameter(rawValue, cc.Column.FieldType));
                            default:
                                throw new Exception("Incorrect clause type");
                        }
                    case InClause ic:
                        var paramValues =
                            from v in ic.Values select sql.AddParameter(v.GetValue(), ic.Column.FieldType);
                        return
                            $"{columnName} {(ic.Negated ? "NOT " : string.Empty)}IN ({string.Join(", ", paramValues)})";
                    case IsNullClause inc:
                        return $"{columnName} IS {(inc.Negated ? "NOT NULL" : "NULL")}";
                    default:
                        throw new Exception("Invalid comparison type.");
                }
            }
            
            switch (clause)
            {
                case BetweenClause bc:
                    return
                        $"{bc.Column.OriginalName} {(bc.Negated ? "NOT " : "")}BETWEEN {sql.AddParameter(bc.LowerValue.GetValue(), bc.Column.FieldType)} AND {sql.AddParameter(bc.UpperValue.GetValue(), bc.Column.FieldType)}";
                case CompoundClause compoundClause:
                    return $"({RenderJoinedClauses(dialect, compoundClause.Children, sql)})";
                case CriteriaClause cc:
                    var rawValue = cc.Value.GetValue();
                    switch (cc.Operation)
                    {
                        case OperationType.Equals:
                        case OperationType.GreaterThan:
                        case OperationType.GreaterThanOrEqual:
                        case OperationType.LessThan:
                        case OperationType.LessThanOrEqual:
                        case OperationType.NotEqual:
                            return RenderComparisonClause(cc.Column.OriginalName, cc.Negated, cc.Operation, sql.AddParameter(rawValue, cc.Column.FieldType));
                        case OperationType.Contains:
                            
                            return RenderLikeClause(dialect, cc, sql, rawValue, "%", "%");
                        case OperationType.StartsWith:
                            return RenderLikeClause(dialect, cc, sql, rawValue, string.Empty, "%");
                        case OperationType.EndsWith:
                            return RenderLikeClause(dialect, cc, sql, rawValue, "%", string.Empty);
                        default:
                            throw new Exception("Incorrect clause type");
                    }
                case InClause ic:
                    var paramValues = from v in ic.Values select sql.AddParameter(v.GetValue(), ic.Column.FieldType);
                    return
                        $"{ic.Column.OriginalName} {(ic.Negated ? "NOT " : string.Empty)}IN ({String.Join(", ", paramValues)})";
                case IsNullClause inc:
                    return $"{inc.Column.OriginalName} IS {(inc.Negated ? "NOT NULL" : "NULL")}";
                default:
                    throw new Exception("Unrecognized clause type.");
            }
        }

        private static readonly Dictionary<OperationType, Tuple<string, string>> CanonicalOps = new Dictionary<OperationType, Tuple<string, string>>
        {
            { OperationType.Equals, new Tuple<string, string>("=", "<>") },
            { OperationType.NotEqual, new Tuple<string, string>("<>", "=") },
            { OperationType.LessThan, new Tuple<string, string>("<", ">=") },
            { OperationType.LessThanOrEqual, new Tuple<string, string>("<=", ">") },
            { OperationType.GreaterThan, new Tuple<string, string>(">", "<=") },
            { OperationType.GreaterThanOrEqual, new Tuple<string, string>(">=", "<") },
        };
        
        private static string RenderComparisonClause(string column, bool negated, OperationType op, string parameter)
        {
            if (!CanonicalOps.TryGetValue(op, out var opstrings))
            {
                throw new Exception($"Invalid comparison type {op}");
            }

            var operationSymbol = negated ? opstrings.Item2 : opstrings.Item1;
            return $"{column} {operationSymbol} {parameter}";
        }

        private static string RenderLikeClause(SqlDialect dialect, CriteriaClause clause, SqlQuery sql, object rawValue,
            string prefix, string suffix)
        {
            if (rawValue?.GetType() != typeof(string))
            {
                throw new StringValueMismatch()
                {
                    RawValue = rawValue,
                };
            }

            var stringValue = rawValue.ToString();

            var likeCommand = dialect == SqlDialect.PostgreSql ? "ILIKE" : "LIKE";
            var escapeCommand = dialect == SqlDialect.MicrosoftSqlServer ? " ESCAPE '\\'" : string.Empty;
            var notCommand = clause.Negated ? "NOT " : "";
            var likeValue = prefix + EscapeLikeValue(stringValue) + suffix;
            return
                $"{clause.Column.OriginalName} {notCommand}{likeCommand} {sql.AddParameter(likeValue, clause.Column.FieldType)}{escapeCommand}";
        }

        private static string EscapeLikeValue(string stringValue)
        {
            var sb = new StringBuilder();
            foreach (var c in stringValue)
            {
                // These characters must be escaped for string queries
                if (c == '\\' || c == '_' || c == '[' || c == ']' || c == '^' || c == '%')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string DecorateIntro(SqlDialect dialect, SearchlightEngine engine)
        {
            var sb = new StringBuilder();
            if (engine.useNoCount && dialect == SqlDialect.MicrosoftSqlServer)
            {
                sb.Append("SET NOCOUNT ON;\n");
            }

            if (engine.useReadUncommitted && dialect == SqlDialect.MicrosoftSqlServer)
            {
                sb.Append("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n");
            }

            return sb.ToString();
        }

        private static string DecorateTableName(SqlDialect dialect, string tableName, SearchlightEngine engine)
        {
            if (engine.useNoLock && dialect == SqlDialect.MicrosoftSqlServer)
            {
                return $"{tableName} WITH (nolock)";
            }

            return tableName;
        }
    }
    /*
     Old code to someday resurface 

    public class DbHelper<KEY, ENTITY>
    {
        private SafeQueryParser _parser;
        private Func<ENTITY, KEY> _primaryKeyFunc;
        private string _sqlTemplate;
        private string _defaultSortColumn;

        /// <summary>
        /// The default maximum size of any fetch is 1000 rows; you can override this by changing the value in the DbHelper.
        /// </summary>
        public const int MAX_RESULT_COUNT = 1000;

        #region Constructors
        /// <summary>
        /// Construct a generic DbHelper to solve basic use cases for an entity
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="sqlTemplate"></param>
        /// <param name="defaultSortColumn"></param>
        /// <param name="tableAlias"></param>
        /// <param name="primaryKeyFunc"></param>
        public DbHelper(DatabaseType databaseType,
            string sqlTemplate,
            string defaultSortColumn,
            string tableAlias,
            Func<ENTITY, KEY> primaryKeyFunc = null)
        {
            _parser = new SafeQueryParser(new EntityColumnDefinitions(typeof(ENTITY)),
                new FullyQualifyColumnNames(tableAlias, databaseType),
                databaseType);
            _sqlTemplate = sqlTemplate;
            _defaultSortColumn = defaultSortColumn;
            TableAlias = tableAlias;
            _primaryKeyFunc = primaryKeyFunc;
            MaxResults = MAX_RESULT_COUNT;
        }

        /// <summary>
        /// Construct a model-renaming DbHelper where the user can filter by model names rather than entity names
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="modelType"></param>
        /// <param name="sqlTemplate"></param>
        /// <param name="defaultSortColumn"></param>
        /// <param name="tableAlias"></param>
        /// <param name="primaryKeyFunc"></param>
        /// <param name="dbFactory"></param>
        public DbHelper(DatabaseType databaseType,
            Type modelType,
            string sqlTemplate,
            string defaultSortColumn,
            string tableAlias,
            Func<ENTITY, KEY> primaryKeyFunc = null)
        {
            _parser = new SafeQueryParser(new ModelColumnDefinitions(modelType),
                new FullyQualifyColumnNames(tableAlias, databaseType),
                databaseType);
            _sqlTemplate = sqlTemplate;
            _defaultSortColumn = defaultSortColumn;
            TableAlias = tableAlias;
            _primaryKeyFunc = primaryKeyFunc;
            MaxResults = MAX_RESULT_COUNT;
        }

        /// <summary>
        /// Number of results to return at maximum
        /// </summary>
        public int MaxResults { get; set; }

        /// <summary>
        /// What is the official table alias for the SQL logic used in this DbHelper
        /// </summary>
        public string TableAlias { get; set; }
        #endregion

        #region Fetch implementation
        /// <summary>
        /// Fetch using the specified fetch request pattern
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="request"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        public FetchResult<ENTITY> Fetch(IDbConnectionFactory factory, FetchRequest request, List<OptionalCommand> commands = null)
        {
            var dp = new DynamicParameters();
            StringBuilder query = new StringBuilder(_sqlTemplate);
            if (commands == null) commands = new List<OptionalCommand>();

            // Generate the list of fields to select
            _parser.ParseSelectClause(request.Include, commands);

            // Give each command a chance to preview the request
            foreach (var c in commands) {
                c.Preview(request);
            }

            // Finalize the query
            var where = _parser.ParseWhereClause(request.Filter);
            var whereClause = where.ValidatedFilter;
            if (!string.IsNullOrEmpty(whereClause)) {
                whereClause = " WHERE " + whereClause;
            }

            // Combine where clause parameters into the ones we've already got
            dp.AddDynamicParams(where.SqlParameters);

            // Query all fields
            var fields = " * ";

            // Determine the sort filter, if any was supplied
            var orderByClause = _parser.ParseOrderByClause(request.SortBy, _defaultSortColumn).Expression;

            // If the user didn't fetch a specific number of results, return this many at maximum
            if (request.MaxResults <= 0) {
                request.MaxResults = MaxResults;
            }

            // Add sql for each optional fetch command that was requested
            bool multi_fetch = false;
            foreach (var command in commands) {
                if (command.IsIncluded) {
                    command.ApplySql(query, dp);
                    var fc = command as MultiFetchCommand<KEY, ENTITY>;
                    if (fc != null) multi_fetch = true;
                }
            }

            // Combine the string with all of the elements
            query = query
                .Replace("{selectClause}", fields)
                .Replace("{whereClause}", whereClause)
                .Replace("{orderByClause}", orderByClause)
                .Replace("{offset}", request.StartIndex.ToString())
                .Replace("{fetchNext}", request.MaxResults.ToString());

            // Enforce delay and logging rules
            var sql = query.ToString();
            DateTime startTime = DateTime.UtcNow;

            // Retrieve objects from the database
            using (var conn = factory.Create()) {
                conn.Open();

                // Multi fetch pattern
                var results = new FetchResult<ENTITY>();
                if (multi_fetch) {
                    using (var multi = conn.QueryMultiple(sql, dp, null, 0, System.Data.CommandType.Text)) {
                        var entities = multi.Read<ENTITY>().ToList();
                        results.value = entities;
                        Dictionary<KEY, ENTITY> dict = null;
                        if (_primaryKeyFunc != null) {
                            dict = results.value.ToDictionary(_primaryKeyFunc);
                        }

                        // Apply all fetch commands
                        foreach (var command in commands) {
                            var fc = command as MultiFetchCommand<KEY, ENTITY>;
                            if (fc != null) {
                                if (fc.IsIncluded) {
                                    fc.ExecuteCommand(entities, dict, multi);
                                }
                            }
                        }
                    }

                // Simple fetch pattern
                } else {
                    var entities = conn.Query<ENTITY>(sql, dp, null, true, null, System.Data.CommandType.Text).ToList();
                    results.value = entities;
                }

                // Construct the final fetch results object, and paginate
                if (results.value == null) results.value = new List<ENTITY>();
                results.count = results.value.Count;

                // Final results hook allows for filtering of the results
                foreach (var command in commands) {
                    if (command.IsIncluded) {
                        command.ApplyResults<ENTITY>(results);
                    }
                }

                // Apply database rules and notify watchers of a query that occurred
                var e = new DapperSqlEventArgs()
                {
                    Duration = DateTime.UtcNow - startTime,
                    RowCount = results.count ?? 0,
                    Sql = sql
                };
                GlobalDbHelperHook?.Invoke(this, e);

                // Here's your result
                return results;
            }
        }
        #endregion

        #region Hooks
        /// <summary>
        /// Hook this event to add functionality to all SQL statements everywhere
        /// </summary>
        public static event EventHandler GlobalDbHelperHook;
        #endregion
    }
    */
}