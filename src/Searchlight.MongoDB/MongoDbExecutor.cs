using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Searchlight;
using Searchlight.Query;


namespace MongoPetSitters
{
    /// <summary>
    /// Static extension class that converts Searchlight syntax trees into MongoDB queries
    /// </summary>
    public static class MongoDbExecutor
    {
        /// <summary>
        /// Run the query represented by this syntax tree against an in-memory collection using LINQ
        /// </summary>
        /// <param name="tree">The syntax tree of the query to execute</param>
        /// <param name="collection">The collection of data elements to query</param>
        /// <typeparam name="T">Generic type of the model</typeparam>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> QueryMongo<T>(this SyntaxTree tree, IMongoCollection<T> collection)
        {
            var filter = BuildMongoFilter<T>(tree.Filter);

            // Sorting and pagination
            var results = await collection.FindAsync(filter, new FindOptions<T, T>
            {
                Sort = BuildMongoSort<T>(tree.OrderBy),
                Skip = (tree.PageNumber != null && tree.PageSize != null) ? (tree.PageNumber * tree.PageSize) : null,
                Limit = tree.PageSize,
            });
            return results.ToEnumerable();
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
            foreach (var clause in clauses)
            {
                switch (clause)
                {
                    case CriteriaClause criteria:
                        var rawValue = criteria.Value.GetValue();
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
                            case OperationType.Contains:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression($"/{rawValue}/"));
                            case OperationType.StartsWith:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression($"/^{rawValue}/"));
                            case OperationType.EndsWith:
                                return Builders<T>.Filter.Regex(criteria.Column.FieldName,
                                    new BsonRegularExpression($"/{rawValue}$/"));
                            default:
                                throw new NotImplementedException();
                        }
                    case InClause inClause:
                        var valueArray = (from v in inClause.Values select v.GetValue()).ToList();
                        return Builders<T>.Filter.In(inClause.Column.FieldName, valueArray);
                    
                    case IsNullClause isNullClause:
                        return Builders<T>.Filter.Eq(isNullClause.Column.FieldName, BsonNull.Value);

                    case BetweenClause betweenClause:
                        var lower = Builders<T>.Filter.Gte(betweenClause.Column.FieldName, betweenClause.LowerValue.GetValue());
                        var upper = Builders<T>.Filter.Lte(betweenClause.Column.FieldName, betweenClause.UpperValue.GetValue());
                        // & operator can be used between Mongo filters
                        return lower & upper;

                    case CompoundClause compoundClause:
                        var innerFilters = BuildMongoFilter<T>(compoundClause.Children);
                        switch (compoundClause.Conjunction)
                        {
                            case ConjunctionType.OR:
                                return Builders<T>.Filter.Or(innerFilters);
                            case ConjunctionType.AND:
                                return Builders<T>.Filter.And(innerFilters);
                            default:
                                throw new NotImplementedException();
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }
    }
}