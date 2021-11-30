using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Searchlight;
using Searchlight.Query;


namespace MongoPetSitters
{
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
            var results = await collection.FindAsync(filter);
            return results.ToEnumerable();
        }

        public static FilterDefinition<T> BuildMongoFilter<T>(List<BaseClause> clauses)
        { 
            foreach (var clause in clauses)
            {
                switch (clause)
                {
                    case CriteriaClause criteria:
                        return criteria.Operation switch
                        {
                            OperationType.Equals => Builders<T>.Filter.Eq(criteria.Column.FieldName, criteria.Value),
                            OperationType.GreaterThan => Builders<T>.Filter.Gt(criteria.Column.FieldName, criteria.Value),
                            OperationType.GreaterThanOrEqual => Builders<T>.Filter.Gte(criteria.Column.FieldName, criteria.Value),
                            OperationType.LessThan => Builders<T>.Filter.Lt(criteria.Column.FieldName, criteria.Value),
                            OperationType.LessThanOrEqual => Builders<T>.Filter.Lte(criteria.Column.FieldName, criteria.Value),
                            OperationType.Contains => Builders<T>.Filter.Text(criteria.Column.FieldName, criteria.Value.ToString()),
                            _ => throw new NotImplementedException(),
                        };
                    case BetweenClause betweenClause:
                        var lower = Builders<T>.Filter.Gte(betweenClause.Column.FieldName, betweenClause.LowerValue);
                        var upper = Builders<T>.Filter.Lte(betweenClause.Column.FieldName, betweenClause.UpperValue);
                        // & operator can be used between Mongo filters
                        return lower & upper;

                    default:
                        throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }
    }
}
