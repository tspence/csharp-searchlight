using System;
using System.Collections.Generic;
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
        public static IEnumerable<T> QueryMongo<T>(this SyntaxTree tree, IMongoCollection<T> collection)
        {
            throw new NotImplementedException();
        }
    }
}
