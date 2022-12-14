using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Searchlight.Exceptions;
using Searchlight.Query;


namespace Searchlight.MongoDB
{
    /// <summary>
    /// Static extension class that converts Searchlight syntax trees into MongoDB queries
    /// </summary>
    public static class MongoDbExecutor
    {
        /// <summary>
        /// Run the query represented by this syntax tree against an in-memory collection using LINQ.
        ///
        /// Limitations:
        /// * MongoDB only performs case-sensitive string comparisons due to a limitation in their driver.
        /// </summary>
        /// <param name="tree">The syntax tree of the query to execute</param>
        /// <param name="collection">The collection of data elements to query</param>
        /// <typeparam name="T">Generic type of the model</typeparam>
        /// <returns></returns>
        public static async Task<FetchResult<T>> QueryMongo<T>(this SyntaxTree tree, IMongoCollection<T> collection)
        {
            // Check for mongo query safety - not all objects can be safely queried!
            if (!MongoModelChecker.IsMongoSafe(tree.Source.ModelType))
            {
                throw new InvalidMongoModel()
                {
                    TableName = tree.Source.TableName,
                };
            }
            
            // Build the filter and sort
            var filter = BuildMongoFilter<T>(tree.Filter);
            var sort = BuildMongoSort<T>(tree.OrderBy);

            // Execute a search
            var results = await collection.FindAsync(filter, new FindOptions<T, T>
            {
                Sort = sort,
                Skip = (tree.PageNumber != null && tree.PageSize != null)
                    ? (tree.PageNumber * tree.PageSize)
                    : null,
                Limit = tree.PageSize,
            });

            // Produce results
            var records = (await results.ToListAsync()).ToArray();
            var totalCount = (await collection.CountDocumentsAsync(filter));
            return new FetchResult<T>()
            {
                totalCount = (int)totalCount,
                pageSize = tree.PageSize,
                pageNumber = tree.PageNumber,
                records = records,
            };
        }

        private static SortDefinition<T> BuildMongoSort<T>(List<SortInfo> orderBy)
        {
            var list = new List<SortDefinition<T>>();
            foreach (var sortInfo in orderBy)
            {
                switch (sortInfo.Direction)
                {
                    case Searchlight.SortDirection.Ascending:
                        list.Add(Builders<T>.Sort.Ascending(sortInfo.Column.FieldName));
                        break;
                    case Searchlight.SortDirection.Descending:
                        list.Add(Builders<T>.Sort.Descending(sortInfo.Column.FieldName));
                        break;
                }
            }
            return Builders<T>.Sort.Combine(list);
        }

        /// <summary>
        /// Construct a MongoDB filter on a set of clause objects
        /// </summary>
        /// <param name="clauses"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static FilterDefinition<T> BuildMongoFilter<T>(List<BaseClause> clauses)
        {
            FilterDefinition<T> filter = null;
            var nextConjunction = ConjunctionType.NONE;
            foreach (var clause in clauses)
            {
                var nextFilter = BuildOneFilter<T>(clause);
                if (clause.Negated)
                {
                    nextFilter = Builders<T>.Filter.Not(nextFilter);
                }
                
                // Merge this into a single filter statement
                if (filter == null)
                {
                    filter = nextFilter;
                }
                else
                {
                    switch (nextConjunction)
                    {
                        case ConjunctionType.OR:
                            filter = Builders<T>.Filter.Or(filter, nextFilter);
                            break;
                        case ConjunctionType.AND:
                            filter = Builders<T>.Filter.And(filter, nextFilter);
                            break;
                        case ConjunctionType.NONE:
                            throw new Exception("Invalid conjunction");
                    }
                }

                nextConjunction = clause.Conjunction;
            }

            return filter ?? FilterDefinition<T>.Empty;
        }

        /// <summary>
        /// This function doesn't handle negation
        /// </summary>
        /// <param name="clause"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static FilterDefinition<T> BuildOneFilter<T>(BaseClause clause)
        {
            switch (clause)
            {
                case CriteriaClause criteria:
                    var rawValue = criteria.Value.GetValue();
                    if (criteria.Column.FieldType == typeof(string))
                    {
                        switch (criteria.Operation)
                        {
                            case OperationType.Contains:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression(Regex.Escape((string)rawValue), "i"));
                            case OperationType.StartsWith:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression($"^{Regex.Escape((string)rawValue)}", "i"));
                            case OperationType.EndsWith:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression($"{Regex.Escape((string)rawValue)}$", "i"));
                            /*
                            default:
                                return StrCaseCmp<T>(criteria.Column.FieldName, (string)rawValue,
                                    criteria.Operation);
                             */
                        }
                    }
                    switch (criteria.Operation)
                    {
                        case OperationType.Equals:
                            return Builders<T>.Filter.Eq(criteria.Column.FieldName, rawValue);
                        case OperationType.NotEqual:
                            return Builders<T>.Filter.Ne(criteria.Column.FieldName, rawValue);
                        case OperationType.GreaterThan:
                            return Builders<T>.Filter.Gt(criteria.Column.FieldName, rawValue);
                        case OperationType.GreaterThanOrEqual:
                            return Builders<T>.Filter.Gte(criteria.Column.FieldName, rawValue);
                        case OperationType.LessThan:
                            return Builders<T>.Filter.Lt(criteria.Column.FieldName, rawValue);
                        case OperationType.LessThanOrEqual:
                            return Builders<T>.Filter.Lte(criteria.Column.FieldName, rawValue);
                        default:
                            throw new NotImplementedException();
                    }
                case InClause inClause:
                    var valueArray = (from v in inClause.Values select v.GetValue()).ToList();
                    return Builders<T>.Filter.In(inClause.Column.FieldName, valueArray);

                case IsNullClause isNullClause:
                    return Builders<T>.Filter.Or(
                        Builders<T>.Filter.Exists("name", false),
                        Builders<T>.Filter.Eq("name", BsonNull.Value),
                        Builders<T>.Filter.Eq("name", (string)null)
                    );

                case BetweenClause betweenClause:
                    var lower = Builders<T>.Filter.Gte(betweenClause.Column.FieldName,
                        betweenClause.LowerValue.GetValue());
                    var upper = Builders<T>.Filter.Lte(betweenClause.Column.FieldName,
                        betweenClause.UpperValue.GetValue());
                    return Builders<T>.Filter.And(lower, upper);

                case CompoundClause compoundClause:
                    return BuildMongoFilter<T>(compoundClause.Children);
                default:
                    throw new NotImplementedException();
            }
        }

        /*
        /// <summary>
        /// This function would eventually permit MongoDB to execute a case insensitive string comparison,
        /// but the C# MongoDB driver hasn't implemented it yet. We'll leave this code present but commented out
        /// until ticket https://jira.mongodb.org/browse/CSHARP-4115 is addressed in some fashion. 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static FilterDefinition<T> StrCaseCmp<T>(
            string fieldName,
            string value,
            OperationType op)
        {
            var parameterObject = Expression.Parameter(typeof(T), "x");
            var fieldExpr = Expression.Property(parameterObject, fieldName);
            var valueExpr = Expression.Constant(value, typeof(string));
            var comparison = Expression.Call(null,
                typeof(string).GetMethod("Compare",
                    new[]
                    {
                        typeof(string), typeof(string), typeof(StringComparison)
                    }),
                fieldExpr, valueExpr, Expression.Constant(StringComparison.OrdinalIgnoreCase));
            var constantZero = Expression.Constant(0);
            BinaryExpression func;
            switch (op)
            {
                case OperationType.GreaterThan:
                    func = Expression.GreaterThan(comparison, constantZero);
                    break;
                case OperationType.GreaterThanOrEqual:
                    func = Expression.GreaterThanOrEqual(comparison, constantZero);
                    break;
                case OperationType.Equals:
                    func = Expression.Equal(comparison, constantZero);
                    break;
                case OperationType.NotEqual:
                    func = Expression.NotEqual(comparison, constantZero);
                    break;
                case OperationType.LessThanOrEqual:
                    func = Expression.LessThanOrEqual(comparison, constantZero);
                    break;
                case OperationType.LessThan:
                    func = Expression.LessThan(comparison, constantZero);
                    break;
                default:
                    throw new NotImplementedException();
            }
            var final = Expression.Lambda<Func<T, bool>>(body: func, parameters: parameterObject);
            // The goal is to produce this filter:
            // { 
            //     $expr: {
            //         $gte: [
            //             { $strcasecmp: [ "$name", "space"] },
            //             0
            //         ] 
            //     }
            // }
            return Builders<T>.Filter.Where(final);
        }
        */
    }
}