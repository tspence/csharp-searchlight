using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Configuration.Default;
using Searchlight.DataSource;
using Searchlight.Exceptions;
using Searchlight.Parsing;
using Searchlight.Query;
using System;

namespace Searchlight.Tests.Parsing
{
    [TestClass]
    public class SafeQueryParserTests
    {
        private SearchlightDataSource _source;

        public SafeQueryParserTests()
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
            _source.Columnifier = new NoColumnify();
            _source.DatabaseType = DataSourceType.Mysql;
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
        public void FilterParseTest()
        {
            //var actual = SafeQueryParser.ParseFilter("a = 'booya' AND b != 1");
            //Assert.AreEqual("a", actual[0].
            //Assert.AreEqual("a = @p1 AND b <> @p2", actual.ValidatedFilter);
            //Assert.AreEqual(2, actual.SqlParameters.ParameterNames.Count());
        }


        [TestMethod]
        public void NullInWhereClause()
        {
            //Assert.AreEqual("a IS NULL", _parser.ParseWhereClause("a is null").ValidatedFilter);
            //Assert.AreEqual("a IS NOT NULL", _parser.ParseWhereClause("a is not null").ValidatedFilter);
            //Assert.AreEqual("(a IS NOT NULL) OR (a IS NULL)", _parser.ParseWhereClause("(a is not null) or (a is null)").ValidatedFilter);
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

        [TestMethod]
        public void AllQueryExpressions()
        {
            //string s;

            //// Try all basic query expression types - should succeed
            //Assert.AreEqual("a = @p1", _parser.ParseWhereClause("a = 'test'").ValidatedFilter);
            //Assert.AreEqual("a = @p1", _parser.ParseWhereClause("a eq 'test'").ValidatedFilter);
            //Assert.AreEqual("a > @p1", _parser.ParseWhereClause("a > 'test'").ValidatedFilter);
            //Assert.AreEqual("a > @p1", _parser.ParseWhereClause("a gt 'test'").ValidatedFilter);
            //Assert.AreEqual("a >= @p1", _parser.ParseWhereClause("a >= 'test'").ValidatedFilter);
            //Assert.AreEqual("a >= @p1", _parser.ParseWhereClause("a ge 'test'").ValidatedFilter);
            //Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a <> 'test'").ValidatedFilter);
            //Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a != 'test'").ValidatedFilter);
            //Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a ne 'test'").ValidatedFilter);
            //Assert.AreEqual("a < @p1", _parser.ParseWhereClause("a < 'test'").ValidatedFilter);
            //Assert.AreEqual("a < @p1", _parser.ParseWhereClause("a lt 'test'").ValidatedFilter);
            //Assert.AreEqual("a <= @p1", _parser.ParseWhereClause("a <= 'test'").ValidatedFilter);
            //Assert.AreEqual("a <= @p1", _parser.ParseWhereClause("a le 'test'").ValidatedFilter);

            //// Try slightly more complex query expression types - should succeed
            //Assert.AreEqual("a BETWEEN @p1 AND @p2", _parser.ParseWhereClause("a between 'test1' and 'test9'").ValidatedFilter);
            //Assert.AreEqual("a IN (@p1, @p2)", _parser.ParseWhereClause("a in ('test', 'test2')").ValidatedFilter);
            //Assert.AreEqual("a LIKE @p1", _parser.ParseWhereClause("a like 'test%'").ValidatedFilter);
            //Assert.AreEqual("a IS NULL", _parser.ParseWhereClause("a is null").ValidatedFilter);
            //Assert.AreEqual("a IS NOT NULL", _parser.ParseWhereClause("a is not null").ValidatedFilter);

            //// Now try some that fail
            //Assert.ThrowsException<ParserSyntaxException>(() => s = _parser.ParseWhereClause("a REALLYSHOULDBE 'test'").ValidatedFilter);
            //Assert.ThrowsException<ParserSyntaxException>(() => s = _parser.ParseWhereClause("a !<= 'test'").ValidatedFilter);
        }

        [TestMethod]
        public void TypeComparisons()
        {
            //// Test the Int64
            //var w = _parser.ParseWhereClause("collong eq 123456789123456");
            //Assert.AreEqual("colLong = @p1", w.ValidatedFilter);
            //Assert.AreEqual(123456789123456, w.SqlParameters.Get<long>("p1"));

            //// Test the guid
            //w = _parser.ParseWhereClause(String.Format("colguid eq '{0}'", Guid.Empty.ToString()));
            //Assert.AreEqual("colGuid = @p1", w.ValidatedFilter);
            //Assert.AreEqual(Guid.Empty, w.SqlParameters.Get<Guid>("p1"));

            //// Test the nullable guid
            //w = _parser.ParseWhereClause(String.Format("colNullableGuid is null or colNullableGuid = '{0}'", Guid.Empty.ToString()));
            //Assert.AreEqual("colNullableGuid IS NULL OR colNullableGuid = @p1", w.ValidatedFilter);
            //Assert.AreEqual(Guid.Empty, w.SqlParameters.Get<Guid>("p1"));

            //// Test the ULONG and nullable ULONG
            //w = _parser.ParseWhereClause("colULong > 12345 or colNullableULong = 6789456");
            //Assert.AreEqual("colULong > @p1 OR colNullableULong = @p2", w.ValidatedFilter);
            //Assert.AreEqual(12345UL, w.SqlParameters.Get<UInt64>("p1"));
            //Assert.AreEqual(6789456UL, w.SqlParameters.Get<Nullable<UInt64>>("p2"));

            //// Test the ULONG and nullable ULONG when compared to a boolean - necessary for redshift
            //w = _parser.ParseWhereClause("colULong = true OR colULong = false");
            //Assert.AreEqual("colULong = @p1 OR colULong = @p2", w.ValidatedFilter);
            //Assert.AreEqual(1UL, w.SqlParameters.Get<UInt64>("p1"));
            //Assert.AreEqual(0UL, w.SqlParameters.Get<UInt64>("p2"));

            //// Nullable variant
            //w = _parser.ParseWhereClause("colNullableULong = true OR colNullableULong = false");
            //Assert.AreEqual("colNullableULong = @p1 OR colNullableULong = @p2", w.ValidatedFilter);
            //Assert.AreEqual(1UL, w.SqlParameters.Get<UInt64>("p1"));
            //Assert.AreEqual(0UL, w.SqlParameters.Get<UInt64>("p2"));
        }
    }
}
