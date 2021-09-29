using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}