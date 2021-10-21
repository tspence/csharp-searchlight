using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoPetSitters;
using Moq;

namespace Searchlight.Tests
{
    [TestClass]
    public class MongoDBExecutorTests
    {
        public interface IMockMongoCollection : IMongoCollection<BsonDocument>
        {
            IFindFluent<BsonDocument, BsonDocument> Find(FilterDefinition<BsonDocument> filter, FindOptions options);

            IFindFluent<BsonDocument, BsonDocument> Project(ProjectionDefinition<BsonDocument, BsonDocument> projection);

            IFindFluent<BsonDocument, BsonDocument> Skip(int skip);

            IFindFluent<BsonDocument, BsonDocument> Limit(int limit);

            IFindFluent<BsonDocument, BsonDocument> Sort(SortDefinition<BsonDocument> sort);
        }
        
        private Mock<IMockMongoCollection > _mockMongoCollection;
        private Mock<IMongoDatabase> _mockMongoDatabase;
        private Mock<IFindFluent<BsonDocument, BsonDocument>> _mockCollectionResult;
        private DataSource src;

        [SearchlightModel(DefaultSort = nameof(name))]
        private class EmployeeObj
        {
            public string name { get; set; }
            public int id { get; set; }
            public DateTime hired { get; set; }
            public decimal paycheck { get; set; }
            public bool onduty { get; set; }
        }
        
        // Adapted from here, trying to see if I can get a mock MongoCollection to query on
        // https://stackoverflow.com/questions/51417459/how-to-mock-imongocollection-find-using-moq
        public MongoDBExecutorTests()
        {
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
            _mockMongoCollection = new Mock<IMockMongoCollection>();
            _mockCollectionResult = new Mock<IFindFluent<BsonDocument, BsonDocument>>();
                _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockMongoDatabase
                .Setup(t => t.GetCollection<BsonDocument>("Test", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockMongoCollection.Object);
        }

        [TestMethod]
        public void CriteriaParseTests()
        {
            // Arrange
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<EmployeeObj>();

            // Act
            var syntax = src.Parse("id eq 1");
            var results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            var filter = results.Render(documentSerializer, serializerRegistry);
            
            // Assert
            Assert.AreEqual(1, filter.GetValue("_id"));
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            
            // Act
            syntax = src.Parse("id > 2");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(2, filter.GetValue("_id").ToBsonDocument().GetValue("$gt"));
            Assert.AreEqual("$gt", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            
            // Act
            syntax = src.Parse("id < 3");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(3, filter.GetValue("_id").ToBsonDocument().GetValue("$lt"));
            Assert.AreEqual("$lt", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            
            // Act
            syntax = src.Parse("id >= 4");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(4, filter.GetValue("_id").ToBsonDocument().GetValue("$gte"));
            Assert.AreEqual("$gte", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            
            // Act
            syntax = src.Parse("id <= 5");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            filter = results.Render(documentSerializer, serializerRegistry);

            // Assert
            Assert.AreEqual(5, filter.GetValue("_id").ToBsonDocument().GetValue("$lte"));
            Assert.AreEqual("$lte", filter.GetValue("_id").ToBsonDocument().Names.FirstOrDefault());
            Assert.AreEqual("_id", filter.Names.FirstOrDefault());
            
            // Act
            syntax = src.Parse("name contains 'New Order'");
            results = MongoDbExecutor.BuildMongoFilter<EmployeeObj>(syntax.Filter);
            // { $text : { "$search" : "name", "$language" : "New Order" } }
            filter = results.Render(documentSerializer, serializerRegistry);
            var inner = filter.GetValue("$text").ToBsonDocument();

            // Assert
            Assert.AreEqual("$text", filter.Names.FirstOrDefault());
            Assert.AreEqual("$search", inner.Names.FirstOrDefault());
            Assert.AreEqual("name", inner.GetValue("$search"));
            Assert.AreEqual("$language", inner.Names.LastOrDefault());
            Assert.AreEqual("New Order", inner.GetValue("$language"));
        }

        [TestMethod]
        public void BetweenParseTest()
        {
            // Arrange
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<EmployeeObj>();
            
            // Act
            var syntax = src.Parse("id between 1 and 5");
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
    }
}