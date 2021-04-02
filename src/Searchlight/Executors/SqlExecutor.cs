using System;
using System.Collections.Generic;
using System.Text;
using Searchlight.Exceptions;
using Searchlight.Parsing;
using Searchlight.Query;

namespace Searchlight.Executors
{
    public class SQLQueryBuilder
    {
        private StringBuilder _sb = new StringBuilder();
        public string whereClause
        {
            get
            {
                return _sb.ToString();
            }
        }
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        public string AddParameter(object p)
        {
            int num = parameters.Count + 1;
            var name = $"@p{num}";
            parameters.Add(name, p);
            return name;
        }

        public void AppendString(string s)
        {
            _sb.Append(s);
        }
    }

    public static class SqlExecutor
    {

        public static SQLQueryBuilder RenderSQL(this SearchlightDataSource source, QueryData query)
        {
            var sql = new SQLQueryBuilder();
            foreach (var clause in query.Filter)
            {
                RenderClause(clause, sql);
            }
            if (sql.parameters.Count > source.MaximumParameters)
            {
                throw new TooManyParametersException(source.MaximumParameters, query.OriginalFilter);
            }
            return sql;
        }

        public static void RenderClause(BaseClause clause, SQLQueryBuilder sql)
        {
            if (clause is BetweenClause)
            {
                var bc = clause as BetweenClause;
                sql.AppendString($"{bc.Column.DatabaseColumn} BETWEEN {sql.AddParameter(bc.LowerValue)} AND {sql.AddParameter(bc.UpperValue)}");
            }
            else if (clause is CompoundClause)
            {
                var cc = clause as CompoundClause;
                sql.AppendString("(");
                foreach (var child in cc.Children)
                {
                    RenderClause(child, sql);
                }
                sql.AppendString(")");
            }
            else if (clause is CriteriaClause)
            {
                var cc = clause as CriteriaClause;
                switch (cc.Operation)
                {
                    case OperationType.Equals: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} = {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.GreaterThan: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} > {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.GreaterThanOrEqual: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} >= {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.LessThan: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} < {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.LessThanOrEqual: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} <= {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.NotEqual: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} <> {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.Like: 
                        sql.AppendString($"{cc.Column.DatabaseColumn} LIKE {sql.AddParameter(cc.Value)}");
                        break;
                    case OperationType.Contains: 
                        if (!(cc.Value is string)) {
                            throw new Exception("Value was not a string type");
                        }
                        sql.AppendString($"{cc.Column.DatabaseColumn} LIKE {sql.AddParameter("%" + cc.Value + "%")}");
                        break;
                    case OperationType.StartsWith: 
                        if (!(cc.Value is string)) {
                            throw new Exception("Value was not a string type");
                        }
                        sql.AppendString($"{cc.Column.DatabaseColumn} LIKE {sql.AddParameter(cc.Value + "%")}");
                        break;
                    case OperationType.EndsWith: 
                        if (!(cc.Value is string)) {
                            throw new Exception("Value was not a string type");
                        }
                        sql.AppendString($"{cc.Column.DatabaseColumn} LIKE {sql.AddParameter("%" + cc.Value)}");
                        break;
                    default: 
                        throw new Exception("Incorrect clause type");
                }
            }
            else if (clause is InClause)
            {
                var ic = clause as InClause;
                sql.AppendString(ic.Column.DatabaseColumn);
                sql.AppendString(" IN (");
                for (int i = 0; i < ic.Values.Count; i++)
                {
                    if (i > 0)
                    {
                        sql.AppendString(", ");
                    }
                    sql.AppendString(sql.AddParameter(ic.Values[i]));
                }
                sql.AppendString(")");

            }
            else if (clause is IsNullClause)
            {
                var inc = clause as IsNullClause;
                sql.AppendString(inc.Column.DatabaseColumn);
                if (inc.Negated)
                {
                    sql.AppendString(" IS NOT NULL");
                }
                else
                {
                    sql.AppendString(" IS NULL");
                }

            }
            else
            {
                throw new Exception("Unrecognized clause type.");
            }

            // If there's another clause after this, add it
            switch (clause.Conjunction)
            {
                case ConjunctionType.AND: sql.AppendString(" AND "); break;
                case ConjunctionType.OR: sql.AppendString(" OR "); break;
            }
        }
    }

    /*
    /// <summary>
    /// Database helper
    /// </summary>
    /// <typeparam name="KEY"></typeparam>
    /// <typeparam name="ENTITY"></typeparam>
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
