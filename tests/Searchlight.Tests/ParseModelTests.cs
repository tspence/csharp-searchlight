using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;
using System.Linq;

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
        public void TestLimitedFields()
        {
            var source = SearchlightDataSource.Create(typeof(TestModelOne), ModelFieldMode.SearchlightOnly);
            var columns = source.ColumnDefinitions.GetColumnDefinitions().ToArray();
            Assert.AreEqual(2, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);

            // Attempt to query a field that does not exist
            string originalFilter = "a = 'test'";
            var ex = Assert.ThrowsException<FieldNameException>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            ex = Assert.ThrowsException<FieldNameException>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("NotASearchlightField", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void TestExpansiveFields()
        {
            var source = SearchlightDataSource.Create(typeof(TestModelOne), ModelFieldMode.All);
            var columns = source.ColumnDefinitions.GetColumnDefinitions().ToArray();
            Assert.AreEqual(3, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);
            Assert.AreEqual("NotASearchlightField", columns[2].FieldName);
            Assert.AreEqual(typeof(string), columns[2].FieldType);

            // Attempt to query a field that does not exist
            string originalFilter = "a = 'test'";
            var ex = Assert.ThrowsException<FieldNameException>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            var clauses = source.ParseFilter(originalFilter);
            Assert.AreEqual(1, clauses.Count());
        }
    }
}
