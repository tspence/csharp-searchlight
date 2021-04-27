using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;
using Searchlight.Query;
using System.Linq;

namespace Searchlight.Tests
{
    [SearchlightModel]
    public class TestStrictMode
    {
        [SearchlightField] public string Name { get; set; }
        [SearchlightField] public string Description { get; set; }
        public string NotASearchlightField { get; set; }
    }

    [SearchlightModel()]
    public class TestFieldRenaming
    {
        [SearchlightField(OriginalName = "field_name")]
        public string Name { get; set; }

        [SearchlightField(Aliases = new string[] {"desription", "DescriptionText"})]
        public string Description { get; set; }

        [SearchlightField] public string NotASearchlightField { get; set; }
    }

    [TestClass]
    public class ParseModelTests
    {
        [TestMethod]
        public void TestLimitedFields()
        {
            var source = DataSource.Create(typeof(TestStrictMode), AttributeMode.Strict);
            var columns = source.GetColumnDefinitions().ToArray();
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
            var source = DataSource.Create(typeof(TestStrictMode), AttributeMode.Loose);
            var columns = source.GetColumnDefinitions().ToArray();
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
            var source = DataSource.Create(typeof(TestFieldRenaming), AttributeMode.Strict);
            var columns = source.GetColumnDefinitions().ToArray();
            Assert.AreEqual(3, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);

            // This field includes backwards compatibility for alternative names
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);
            Assert.AreEqual(2, columns[1].Aliases.Length);
            Assert.AreEqual("desription",
                columns[1].Aliases[
                    0]); // Example: "I misspelled the field name in version 1 of the API, so I had to rename it"
            Assert.AreEqual("DescriptionText",
                columns[1].Aliases[
                    1]); // Example: "This field was originally known as DescriptionText, but our new standards made us change it"

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

        [SearchlightModel]
        public class TestFieldConflicts
        {
            [SearchlightField(Aliases = new string[] {"description"})]
            public string Name { get; set; }

            [SearchlightField(Aliases = new string[] {"desription", "DescriptionText"})]
            public string Description { get; set; }
        }

        [TestMethod]
        public void TestNamingConflicts()
        {
            var ex = Assert.ThrowsException<DuplicateName>(() =>
            {
                var source = DataSource.Create(typeof(TestFieldConflicts), AttributeMode.Strict);
            });
            Assert.AreEqual("DESCRIPTION", ex.ConflictingName);
            Assert.AreEqual("Name", ex.ExistingColumn);
            Assert.AreEqual("Description", ex.ConflictingColumn);
        }

        [TestMethod]
        public void TestNonSearchlightModel()
        {
            // "THIS" isn't a searchlight model; in strict mode it doesn't work
            var ex = Assert.ThrowsException<NonSearchlightModel>(() =>
            {
                var source = DataSource.Create(this.GetType(), AttributeMode.Strict);
            });

            // But if I try it in loose mode, anything goes
            var s2 = DataSource.Create(this.GetType(), AttributeMode.Loose);
            Assert.IsNotNull(s2);
        }

        [SearchlightModel(DefaultSort = "name")]
        public class TestWithDefaultSort
        {
            [SearchlightField(Aliases = new string[] {"fullName"})]
            public string Name { get; set; }

            [SearchlightField(Aliases = new string[] {"DescriptionText"})]
            public string Description { get; set; }
        }

        [TestMethod]
        public void TestDefaultSort()
        {
            var source = DataSource.Create(typeof(TestWithDefaultSort), AttributeMode.Strict);
            var query = source.Parse(null, null, null);
            Assert.AreEqual(1, query.OrderBy.Count);
            Assert.AreEqual("Name", query.OrderBy[0].Column.FieldName);
            Assert.AreEqual(SortDirection.Ascending, query.OrderBy[0].Direction);

            // Now try the same thing with a class that doesn't have a default sort
            var source2 = DataSource.Create(typeof(TestStrictMode), AttributeMode.Strict);
            var query2 = source2.Parse(null, null, null);
            Assert.AreEqual(0, query2.OrderBy.Count);
        }
    }
}