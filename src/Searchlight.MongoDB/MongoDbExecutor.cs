using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
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

        private static FilterDefinition<T> BuildMongoFilter<T>(List<BaseClause> clauses)
        { 
            foreach (var clause in clauses)
            {
                switch (clause)
                {
                    case CriteriaClause criteria:
                        switch (criteria.Operation)
                        {
                            case Searchlight.OperationType.Equals:
                                return Builders<T>.Filter.Eq(criteria.Column.FieldName, criteria.Value);

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
