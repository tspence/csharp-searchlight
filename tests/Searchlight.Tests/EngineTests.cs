﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Exceptions;

namespace Searchlight.Tests
{
    [TestClass]
    public class EngineTests
    {

        [SearchlightModel(Aliases = new string[] {"TestAlias1", "TestAlias2" }, DefaultSort = nameof(Name))]
        public class TestTableAliases
        {
            [SearchlightField]
            public string Name { get; set; }
            [SearchlightField]
            public string Description { get; set; }
        }
        
        [TestMethod]
        public void TestTableNameAliases()
        {
            var engine = new SearchlightEngine().AddClass(typeof(TestTableAliases));
            Assert.IsNotNull(engine.FindTable("TestAlias1"));
            Assert.IsNotNull(engine.FindTable("TestAlias2"));
            Assert.IsNotNull(engine.FindTable("TestTableAliases"));
            Assert.AreEqual(engine.FindTable("TestTableAliases"), engine.FindTable("TestAlias1"));
        }
        
        [TestMethod]
        public void Test_MaxPSUpdatedFromDefault()
        {
            var engine = new SearchlightEngine() { MaximumPageSize = 50 };
            Assert.IsTrue(engine.MaximumPageSize == 50);
        }

        private FetchRequest mockFetchRequest = new FetchRequest
        {
            table = "tableau",
            include = "include me",
            filter = "",
        };

        [TestMethod]
        public void Test_SettingRequestPageSizeWhenNotSpecified()
        {
            var engine = new SearchlightEngine();
            engine.Parse(mockFetchRequest);
            Assert.IsTrue(mockFetchRequest.pageSize == engine.MaximumPageSize); }

        [TestMethod]
        public void Test_ParsingRequestWithInvalidPageSize()
        {
            mockFetchRequest.pageSize = 2000;
            var engine = new SearchlightEngine();
            engine.MaximumPageSize = 1000;
            Assert.ThrowsException<InvalidPageSize>(() => engine.Parse(mockFetchRequest));
        }

        [TestMethod]
        public void Test_DefaultPageSize()
        {
            var engine = new SearchlightEngine().AddClass(typeof(TestTableAliases));
            engine.DefaultPageSize = 200;
            var fetchRequest = new FetchRequest()
            {
                table = "TestTableAliases",
                filter = "Name startswith a",
            };
            var syntax = engine.Parse(fetchRequest);
            Assert.AreEqual(200, syntax.PageSize);
        }
    }
}