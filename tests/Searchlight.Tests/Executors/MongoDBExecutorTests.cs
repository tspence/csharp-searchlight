using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EphemeralMongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Searchlight.MongoDB;
using Searchlight.Query;
using Searchlight.Tests.Models;

namespace Searchlight.Tests.Executors
{
    [TestClass]
    public class MongoDbExecutorTests
    {
        private DataSource _src;
        private IMongoRunner _runner;
        private IMongoCollection<EmployeeObj> _collection;
        private List<EmployeeObj> _list;
        private Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _mongo;

        [TestInitialize]
        public async Task SetupMongoClient()
        {
            _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
            var options = new MongoRunnerOptions();
            _runner = MongoRunner.Run(options);

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            _collection = database.GetCollection<EmployeeObj>("TestCollection");
            _list = EmployeeObj.GetTestList();
            await _collection.InsertManyAsync(_list);
            _mongo = syntax => syntax.QueryMongo(_collection);
        }

        [TestCleanup]
        public void CleanupMongo()
        {
            _collection = null;
            if (_runner != null)
            {
                _runner.Dispose();
            }
        }

        [TestMethod]
        public async Task EmployeeTestSuite()
        {
            await Executors.EmployeeTestSuite.BasicTestSuite(_src, _list, _mongo);
            // MongoDB can't do case insensitive string comparisons.
            // await Executors.EmployeeTestSuite.CaseInsensitiveStringTestSuite(_src, _list, _mongo);
        }
        
        // ================================================================================
        // Below this line are MongoDB specific tests
        // TODO: The MongoDB executor is only able to do case sensitive string comparison. 
        //       Please update all assertions when this issue is addressed, OR just add
        //       EmployeeTests.CaseInsensitiveStringTestSuite to the suite above.
        // ================================================================================

        [TestMethod]
        public void TestMongoSafety()
        {
            Assert.IsTrue(MongoModelChecker.IsMongoSafe(typeof(EmployeeObj)));
            Assert.IsFalse(MongoModelChecker.IsMongoSafe(typeof(IncompatibleEmployeeObj)));
        }

        [TestMethod]
        public void CriteriaParseTests()
        {
            // Arrange
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<EmployeeObj>();

            // Act
            var syntax = _src.Parse("id eq 1");
            var results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            var filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(1, filter.GetValue("_id"));
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());

            // Act
            syntax = _src.Parse("id > 2");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(2, filter.GetValue("_id").ToBsonDocument().GetValue("$gt"));
            Assert.AreEqual("$gt", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());

            // Act
            syntax = _src.Parse("id < 3");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(3, filter.GetValue("_id").ToBsonDocument().GetValue("$lt"));
            Assert.AreEqual("$lt", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());

            // Act
            syntax = _src.Parse("id >= 4");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(4, filter.GetValue("_id").ToBsonDocument().GetValue("$gte"));
            Assert.AreEqual("$gte", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());

            // Act
            syntax = _src.Parse("id <= 5");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(5, filter.GetValue("_id").ToBsonDocument().GetValue("$lte"));
            Assert.AreEqual("$lte", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());

            // Act
            syntax = _src.Parse("name contains 'New Order'");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            // { $text : { "$search" : "name", "$language" : "New Order" } }
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual("name", filter.Names.FirstOrDefault());
            Assert.AreEqual(new BsonRegularExpression("New\\ Order", "i"), filter.GetValue("name"));
        }

        [TestMethod]
        public void BetweenParseTest()
        {
            // Arrange
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<EmployeeObj>();

            // Act
            var syntax = _src.Parse("id between 1 and 5");
            var results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            // { "_id" : { "$gte" : 1, "$lte" : 5 } }
            var filter = results.Render(documentSerializer, serializerRegistry);
            var inner = filter.GetValue("_id").ToBsonDocument();

            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            Assert.AreEqual("$gte", inner.Names.FirstOrDefault());
            Assert.AreEqual(1, inner.GetValue("$gte"));
            Assert.AreEqual("$lte", inner.Names.LastOrDefault());
            Assert.AreEqual(5, inner.GetValue("$lte"));
        }

        [TestMethod]
        public async Task GreaterThanQuery()
        {
            var syntax = _src.Parse("name gt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            // TODO: MongoDB string comparisons are case sensitive.  When this is corrected, update assertions
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) > 0);
            }
        }

        [TestMethod]
        public async Task GreaterThanOrEqualQuery()
        {
            var syntax = _src.Parse("name ge 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThanOrEqual, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.IsNotNull(results);

            // TODO: MongoDB string comparisons are case sensitive.  When this is corrected, update assertions
            Assert.AreEqual(0, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers",
                    StringComparison.CurrentCultureIgnoreCase) >= 0);
            }
        }

        [TestMethod]
        public async Task LessThanQuery()
        {
            var syntax = _src.Parse("name lt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            // TODO: MongoDB string comparisons are case sensitive.  When this is corrected, update assertions
            Assert.AreEqual(9, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.Ordinal) < 0);
            }
        }

        [TestMethod]
        public async Task LessThanOrEqualQuery()
        {
            var syntax = _src.Parse("name le 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            // TODO: MongoDB string comparisons are case sensitive.  When this is corrected, update assertions
            Assert.AreEqual(9, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers",
                    StringComparison.Ordinal) <= 0);
            }
        }
        
        [TestMethod]
        public async Task StringEqualsCaseInsensitive()
        {
            var syntax = _src.Parse("name eq 'ALICE SMITH'");

            // TODO: MongoDB string comparisons are case sensitive.  When this is corrected, update assertions
            var result = await syntax.QueryMongo(_collection);

            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.records.Length);

            // Try the inverse
            syntax = _src.Parse("name not eq 'ALICE SMITH'");
            result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(_list.Count, result.records.Length);
        }
        
        [TestMethod]
        public async Task BasicCriteria()
        {
            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and paycheck le 1000");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause)syntax.Filter[0]).Value.GetValue());
            Assert.AreEqual("paycheck", ((CriteriaClause)syntax.Filter[1]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause)syntax.Filter[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)syntax.Filter[1]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(8, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck <= 1000.0m);
            }
        }
    }
}