using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoPetSitters;
using Searchlight.Query;

namespace Searchlight.Tests
{
    [TestClass]
    public class MongoDBExecutorTests
    {
        private DataSource _src;
        private MongoDbRunner _runner;
        private IMongoCollection<EmployeeObj> _collection;
        private List<EmployeeObj> _referenceList;

        [TestInitialize()]
        public async Task SetupMongoClient()
        {
            _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
            _runner = MongoDbRunner.Start();

            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("IntegrationTest");
            _collection = database.GetCollection<EmployeeObj>("TestCollection");
            _referenceList = EmployeeObj.GetTestList();
            await _collection.InsertManyAsync(_referenceList);
        }

        [TestCleanup]
        public void CleanupMongo()
        {
            _collection = null;
            _runner.Dispose();
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
            Assert.AreEqual(new BsonRegularExpression("/New Order/"), filter.GetValue("name"));
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
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck <= 1000.0m);
            }
        }


        [TestMethod]
        public async Task NestedClauseQuery()
        {
            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and (paycheck lt 1000 or paycheck gt 1000)");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Did we get a nested clause?
            var cc = syntax.Filter[1] as CompoundClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual(2, cc.Children.Count);
            Assert.AreEqual("paycheck", ((CriteriaClause)cc.Children[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause)cc.Children[0]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)cc.Children[0]).Value.GetValue());
            Assert.AreEqual("paycheck", ((CriteriaClause)cc.Children[1]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)cc.Children[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)cc.Children[1]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(6, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck is 800.0m or 1200.0m or 10.0m or 578.00m or 123.00m or 987.00m);
            }
        }

        [TestMethod]
        public async Task BetweenQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("id between 2 and 4");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(false, syntax.Filter[0].Negated);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((BetweenClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause)syntax.Filter[0]).LowerValue.GetValue());
            Assert.AreEqual(4, ((BetweenClause)syntax.Filter[0]).UpperValue.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(3, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.id < 5);
            }

            // Test the opposite
            syntax = _src.Parse("id not between 2 and 4");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(true, syntax.Filter[0].Negated);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((BetweenClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause)syntax.Filter[0]).LowerValue.GetValue());
            Assert.AreEqual(4, ((BetweenClause)syntax.Filter[0]).UpperValue.GetValue());
            results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(6, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id is <= 1 or >= 5);
            }
        }


        [TestMethod]
        public async Task StartsWithQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name startswith 'A'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.StartsWith, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("A", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name[0] == 'A');
            }
        }


        [TestMethod]
        public async Task EndsWithQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name endswith 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.EndsWith, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(2, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name.EndsWith("s", StringComparison.OrdinalIgnoreCase));
            }
        }


        [TestMethod]
        public async Task ContainsQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name contains 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.Contains, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(8, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e != null && e.name.Contains('s', StringComparison.OrdinalIgnoreCase));
            }

            // Now test the opposite
            syntax = _src.Parse("name not contains 's'");
            results = await syntax.QueryMongo(_collection);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(
                    e != null && (e.name == null || !e.name.Contains('s', StringComparison.OrdinalIgnoreCase)));
            }
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
            Assert.AreEqual(7, results.records.Length);
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
            Assert.AreEqual(7, results.records.Length);
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
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) < 0);
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
            Assert.AreEqual(2, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers",
                    StringComparison.CurrentCultureIgnoreCase) <= 0);
            }
        }

        [TestMethod]
        public async Task NotEqualQuery()
        {
            var syntax = _src.Parse("Name != 'Alice Smith'");

            var result = await syntax.QueryMongo(_collection);

            Assert.AreEqual(_referenceList.Count - 1, result.records.Length);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
        }


        [TestMethod]
        public void BooleanContains()
        {
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty contains 's'"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty contains True"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty startswith True"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty endswith True"); });
        }


        [TestMethod]
        public async Task IsNullQuery()
        {
            // var filter = Builders<EmployeeObj>.Filter.And(
            //     Builders<EmployeeObj>.Filter.Exists("name", true),
            //     Builders<EmployeeObj>.Filter.Ne("name", BsonNull.Value),
            //     Builders<EmployeeObj>.Filter.Ne("name", (string)null)
            //     );
            // var matches = await (await _collection.FindAsync(filter)).ToListAsync();
            // Assert.IsNotNull(matches);
            // Assert.AreEqual(8, matches.Count);
            
            var syntax = _src.Parse("Name is NULL");
            var result = await syntax.QueryMongo(_collection);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(1, result.records.Length);

            // Test the opposite
            syntax = _src.Parse("Name is not NULL");
            result = await syntax.QueryMongo(_collection);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(8, result.records.Length);
        }


        [TestMethod]
        public async Task ContainsNull()
        {
            // Searchlight interprets the word "null" without apostrophes here to be the string value "null"
            // instead of a null.
            var syntax = _src.Parse("Name contains null");

            var result = await syntax.QueryMongo(_collection);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);
            Assert.IsTrue(result.records.Any(p => p.name == "Roderick 'null' Sqlkeywordtest"));
        }


        [TestMethod]
        public async Task InQuery()
        {
            var syntax = _src.Parse("name in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')");

            var result = await syntax.QueryMongo(_collection);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsTrue(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);

            // Now run the opposite query
            syntax = _src.Parse("name not in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')");
            result = await syntax.QueryMongo(_collection);

            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsFalse(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(7, result.records.Length);
        }


        [TestMethod]
        public async Task InQueryInt()
        {
            // getting not implemented error on this line
            // make sure using right formatting, if so then in operator needs adjustment
            var syntax = _src.Parse("id in (1,2,57)");

            var result = await syntax.QueryMongo(_collection);

            Assert.IsTrue(result.records.Any(p => p.id == 1));
            Assert.IsTrue(result.records.Any(p => p.id == 2));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);
        }

        [TestMethod]
        public async Task InQueryDecimals()
        {
            var syntax = _src.Parse("paycheck in (578.00, 1.234)");

            var result = await syntax.QueryMongo(_collection);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records[0].id == 7);
        }

        [TestMethod]
        public void InQueryEmptyList()
        {
            Assert.ThrowsException<EmptyClause>(() => _src.Parse("name in ()"));
            Assert.ThrowsException<EmptyClause>(() => _src.Parse("paycheck > 1 AND name in ()"));
        }

        [TestMethod]
        public async Task StringEqualsCaseInsensitive()
        {
            var syntax = _src.Parse("name eq 'ALICE SMITH'");

            var result = await syntax.QueryMongo(_collection);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);

            // Try the inverse
            syntax = _src.Parse("name not eq 'ALICE SMITH'");
            result = await syntax.QueryMongo(_collection);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(_referenceList.Count - 1, result.records.Length);
        }

        [TestMethod]
        public async Task DefinedDateOperators()
        {
            var syntax = _src.Parse("hired < TODAY");
            var result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Length == 3 || result.records.Length == 4);

            syntax = _src.Parse("hired < TOMORROW");
            result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);

            syntax = _src.Parse("hired < tomorrow");
            result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);

            syntax = _src.Parse("hired > YESTERDAY");
            result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);

            syntax = _src.Parse("hired > NOW");
            result = await syntax.QueryMongo(_collection);
            Assert.AreEqual(4, result.records.Length);

            syntax = _src.Parse("hired < NOW");
            result = await syntax.QueryMongo(_collection);
            Assert.AreEqual(5, result.records.Length);

            Assert.ThrowsException<FieldTypeMismatch>(() => _src.Parse("hired > yesteryear"));
        }

        [TestMethod]
        public async Task NormalDateQueries()
        {
            var syntax = _src.Parse("hired > 2020-01-01");
            var result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == _referenceList.Count);

            syntax = _src.Parse("hired < 1985-01-01");
            result = await syntax.QueryMongo(_collection);
            Assert.IsFalse(result.records.Any());

            // Now try the opposite
            syntax = _src.Parse("hired not < 1985-01-01");
            result = await syntax.QueryMongo(_collection);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == _referenceList.Count);

            syntax = _src.Parse("hired not > 2020-01-01");
            result = await syntax.QueryMongo(_collection);
            Assert.IsFalse(result.records.Any());
        }

        [TestMethod]
        public async Task SortedQueries()
        {
            // id test ascending and descending

            var control = (from item in _referenceList orderby item.id ascending select item).ToList();
            var syntax = _src.Parse(null, null, "id ASC");
            var result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }

            control = (from item in _referenceList orderby item.id descending select item).ToList();
            syntax = _src.Parse("", null, "id descending");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }

            // name test ascending and descending
            control = (from item in _referenceList orderby item.name ascending select item).ToList();
            syntax = _src.Parse("", null, "name ASC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }

            control = (from item in _referenceList orderby item.name descending select item).ToList();
            syntax = _src.Parse("", null, "name DESC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }

            // paycheck test ascending and descending
            control = (from item in _referenceList orderby item.paycheck ascending select item).ToList();
            syntax = _src.Parse("", null, "paycheck ASC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }

            control = (from item in _referenceList orderby item.paycheck descending select item).ToList();
            syntax = _src.Parse("", null, "paycheck DESC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }

            // onduty test ascending and descending
            control = (from item in _referenceList orderby item.onduty ascending select item).ToList();
            syntax = _src.Parse("", null, "onduty ASC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }

            control = (from item in _referenceList orderby item.onduty descending select item).ToList();
            syntax = _src.Parse("", null, "onduty DESC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }

            // hired test ascending and descending
            control = (from item in _referenceList orderby item.hired ascending select item).ToList();
            syntax = _src.Parse("", null, "hired ASC");
            result = await syntax.QueryMongo(_collection);

            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].hired, control[i].hired);
            }

            control = (from item in _referenceList orderby item.hired descending select item).ToList();
            syntax = _src.Parse("", null, "hired DESC");
            result = await syntax.QueryMongo(_collection);
            for (var i = 0; i < _referenceList.Count; i++)
            {
                Assert.AreEqual(result.records[i].hired, control[i].hired);
            }
        }

        [TestMethod]
        public async Task DefaultReturn()
        {
            var syntax = _src.Parse("");
            syntax.PageNumber = 0; // default is 0
            syntax.PageSize = 0; // default is 0

            var result = await syntax.QueryMongo(_collection);

            // return everything
            Assert.AreEqual(_referenceList.Count, result.records.Length);
        }

        [TestMethod]
        public async Task PageNumberNoPageSize()
        {
            var syntax = _src.Parse("");
            syntax.PageNumber = 2;
            syntax.PageSize = 0; // default is 0

            var result = await syntax.QueryMongo(_collection);

            // return everything
            Assert.AreEqual(result.records.Length, _referenceList.Count);
        }

        [TestMethod]
        public async Task PageSizeNoPageNumber()
        {
            var syntax = _src.Parse("");

            syntax.PageSize = 2;
            syntax.PageNumber = 0; // no page number defaults to 0

            var result = await syntax.QueryMongo(_collection);

            // should take the first 2 elements
            Assert.AreEqual(result.records.Length, 2);
        }

        [TestMethod]
        public async Task PageSizeAndPageNumber()
        {
            var syntax = _src.Parse("");
            syntax.PageSize = 1;
            syntax.PageNumber = 2;

            var result = await syntax.QueryMongo(_collection);

            Assert.AreEqual(result.records.Length, 1);
        }

        [TestMethod]
        public void TestMongoSafety()
        {
            Assert.IsTrue(MongoModelChecker.IsMongoSafe(typeof(EmployeeObj)));
            Assert.IsFalse(MongoModelChecker.IsMongoSafe(typeof(IncompatibleEmployeeObj)));
        }
    }
}