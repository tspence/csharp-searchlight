using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoPetSitters;
using Moq;

namespace Searchlight.Tests
{
    [TestClass]
    public class MongoDBExecutorTests
    {
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
        
        public MongoDBExecutorTests()
        {
            src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
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