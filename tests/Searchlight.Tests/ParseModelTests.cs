using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;

namespace Searchlight.Tests
{
    [SearchlightModel]
    public class TestModelOne
    {
        [SearchlightField]
        public string Name { get; set; }
        [SearchlightField]
        public string Description { get; set; }
        public string NotASearchlightField { get; set; }
    }

    public class InvalidModel
    {

    }

    [TestClass]
    public class ParseModelTests
    {

        [TestMethod]
        public void TestModelOne()
        {
            var source = SearchlightDataSource.
            string originalFilter = "a = 'test' and b = 'Hello!'";
            var ex = Assert.ThrowsException<FieldValueException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual("b", ex.FieldName);
            Assert.AreEqual("System.Int32", ex.FieldType);
            Assert.AreEqual("Hello!", ex.FieldValue);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }
    }
}
