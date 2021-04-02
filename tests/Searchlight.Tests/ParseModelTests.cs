using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;
using Searchlight.Query;
using System.Linq;

namespace Searchlight.Tests
{
    [SearchlightModel]
    public class TestStrictMode
    {
        [SearchlightField]
        public string Name { get; set; }
        [SearchlightField]
        public string Description { get; set; }
        public string NotASearchlightField { get; set; }
    }

    public class TestFieldRenaming
    {
        [SearchlightField(originalName: "field_name")]
        public string Name { get; set; }
        [SearchlightField(aliases: new string[] { "desription", "DescriptionText" })]
        public string Description { get; set; }
        [SearchlightField]
        public string NotASearchlightField { get; set; }

    }

    [TestClass]
    public class ParseModelTests
    {

        [TestMethod]
        public void TestLimitedFields()
        {
            var source = SearchlightDataSource.Create(typeof(TestStrictMode), ModelFieldMode.Strict);
            var columns = source.ColumnDefinitions.GetColumnDefinitions().ToArray();
            Assert.AreEqual(2, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);

            // Attempt to query a field that does not exist
            string originalFilter = "a = 'test'";
            var ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("NotASearchlightField", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void TestExpansiveFields()
        {
            var source = SearchlightDataSource.Create(typeof(TestStrictMode), ModelFieldMode.Loose);
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
            var ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            var clauses = source.ParseFilter(originalFilter);
            Assert.AreEqual(1, clauses.Count());
            var cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("NotASearchlightField", cc.Column.FieldName);
        }

        [TestMethod]
        public void TestFieldRenaming()
        {
            var source = SearchlightDataSource.Create(typeof(TestFieldRenaming), ModelFieldMode.Strict);
            var columns = source.ColumnDefinitions.GetColumnDefinitions().ToArray();
            Assert.AreEqual(3, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);

            // This field includes backwards compatibility for alternative names
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);
            Assert.AreEqual(2, columns[1].Aliases.Length);
            Assert.AreEqual("desription", columns[1].Aliases[0]); // Example: "I misspelled the field name in version 1 of the API, so I had to rename it"
            Assert.AreEqual("DescriptionText", columns[1].Aliases[1]); // Example: "This field was originally known as DescriptionText, but our new standards made us change it"

            // Attempt to query a field using its old name
            var clauses = source.ParseFilter("desription contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count());
            var cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);

            // Attempt to query a field using its old name
            clauses = source.ParseFilter("DescriptionText contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count());
            cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);

            // Attempt to query a field using its old name
            clauses = source.ParseFilter("Description contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count());
            cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);
        }
    }
}
