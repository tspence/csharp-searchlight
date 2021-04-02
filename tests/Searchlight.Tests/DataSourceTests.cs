using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Configuration.Default;
using Searchlight;
using Searchlight.Parsing;
using Searchlight.Query;
using System;

namespace Searchlight.Tests
{
    [TestClass]
    public class DataSourceTests
    {
        private SearchlightDataSource _source;

        public DataSourceTests()
        {
            _source = new SearchlightDataSource();
            _source.ColumnDefinitions = new CustomColumnDefinition()
                .WithColumn("a", typeof(String), null)
                .WithColumn("b", typeof(Int32), null)
                .WithColumn("colLong", typeof(Int64), null)
                .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
                .WithColumn("colULong", typeof(UInt64), null)
                .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
                .WithColumn("colGuid", typeof(Guid), null);
        }

        [TestMethod]
        public void IncorrectFieldValueType()
        {
            string originalFilter = "a = 'test' and b = 'Hello!'";
            var ex = Assert.ThrowsException<FieldValueException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual("b", ex.FieldName);
            Assert.AreEqual("System.Int32", ex.FieldType);
            Assert.AreEqual("Hello!", ex.FieldValue);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void AllParenthesis()
        {
            // Basic problem: if you never close a parenthesis that's a syntax error
            var ex1 = Assert.ThrowsException<OpenClauseException>(() => _source.ParseFilter("((((((((((("));

            // If you unbalance your parenthesis, that's a syntax error
            var ex2 = Assert.ThrowsException<OpenClauseException>(() => _source.ParseFilter("(((((((((((a = 'hi')))"));

            // if you have too many closing parens, it would expect AND or OR instead of another close paren
            var ex3 = Assert.ThrowsException<UnknownConjunctionException>(() => _source.ParseFilter("(((a = 'hi'))))))))))))"));

            // If you forget to supply any actual criteria, it reads the closing paren and thinks its a field name
            var ex4 = Assert.ThrowsException<FieldNameException>(() => _source.ParseFilter("()"));
        }

        [DataTestMethod]
        [DataRow("a, b DESC")]
        [DataRow("a AsC, b DESc")]
        [DataRow("a asc   , b    DESC    ")]
        public void OrderByCasingAndSpacing(string orderby)
        {
            var result = _source.ParseOrderBy(orderby);
            Assert.AreEqual("a", result[0].Column.FieldName);
            Assert.AreEqual(SortDirection.Ascending, result[0].Direction);
            Assert.AreEqual("b", result[1].Column.FieldName);
            Assert.AreEqual(SortDirection.Descending, result[1].Direction);
        }

        [TestMethod]
        public void OrderByExceptionsTest()
        {
            // Field doesn't exist
            Assert.ThrowsException<FieldNameException>(() => _source.ParseOrderBy("c, d ASC"));

            // No comma between fields
            Assert.ThrowsException<ParserSyntaxException>(() => _source.ParseOrderBy("a b DESC"));

            // Trailing comma
            Assert.ThrowsException<TrailingConjunctionException>(() => _source.ParseOrderBy("a, b,"));
        }

        [TestMethod]
        public void OnlyConjunctions()
        {
            // Silly example
            Assert.ThrowsException<FieldNameException>(() =>
            {
                var clauses = _source.ParseFilter("AND ( ) OR ");
            });

            // Realistic example of a forgetful customer
            Assert.ThrowsException<TrailingConjunctionException>(() => {
                _source.ParseFilter("a = b OR ");
            });

            // Realistic example of a quirky but valid customer request
            var clauses = _source.ParseFilter("(a = 'test' OR b = 1)");
            Assert.IsTrue(clauses[0] is CompoundClause);
            Assert.AreEqual(clauses.Count, 1);
            var cc = clauses[0] as CompoundClause;
            Assert.AreEqual(cc.Children.Count, 2);
            Assert.IsTrue(cc.Children[0] is CriteriaClause);
            Assert.AreEqual(cc.Children[0].Conjunction, ConjunctionType.OR);
            Assert.IsTrue(cc.Children[1] is CriteriaClause);
            Assert.AreEqual(cc.Children[1].Conjunction, ConjunctionType.NONE);
        }
    }
}
