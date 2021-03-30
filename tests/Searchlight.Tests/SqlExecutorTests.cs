using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Configuration.Default;
using Searchlight.DataSource;
using Searchlight.Exceptions;
using Searchlight.Executors;

namespace Searchlight.Tests
{
    [TestClass]
    public class SqlExecutorTests
    {
        private SearchlightDataSource _source;

        public SqlExecutorTests()
        {
            var safeColumns = new CustomColumnDefinition()
                .WithColumn("a", typeof(String), null)
                .WithColumn("b", typeof(Int32), null)
                .WithColumn("colLong", typeof(Int64), null)
                .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
                .WithColumn("colULong", typeof(UInt64), null)
                .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
                .WithColumn("colGuid", typeof(Guid), null);

            _source = new SearchlightDataSource();
            _source.ColumnDefinitions = safeColumns;
            _source.Columnifier = new NoColumnify();
            _source.MaximumParameters = 200;
            _source.DefaultSortField = "a";
        }

        [TestMethod]
        public void TooManyParameters()
        {
            string originalFilter = "b in (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256)";
            var ex = Assert.ThrowsException<TooManyParametersException>(() =>
            {
                var query = _source.Parse(null, originalFilter, null);
                var sql = SqlExecutor.RenderSQL(_source, query);
            });
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void CaseSensitivity()
        {
            // First test, lowercase
            var query = _source.Parse(null, "b between 1 and 5", null);
            var sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.whereClause);
            Assert.AreEqual(2, sql.parameters.Count());

            // Second test, proper case
            query = _source.Parse(null, "b Between 1 And 5", null);
            sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.whereClause);
            Assert.AreEqual(2, sql.parameters.Count());

            // Third test, uppercase
            query = _source.Parse(null, "b BETWEEN 1 AND 5", null);
            sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.whereClause);
            Assert.AreEqual(2, sql.parameters.Count());
        }

        [TestMethod]
        public void CommonSyntaxErrors()
        {
            // Error in "IN" clause
            string originalFilter = "b in (1, 2>*#]";
            var ex = Assert.ThrowsException<ParserSyntaxException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual(">", ex.BadToken);
            Assert.AreEqual(2, ex.ExpectedTokens.Count());
            Assert.IsTrue(ex.ExpectedTokens.Contains(","));
            Assert.IsTrue(ex.ExpectedTokens.Contains(")"));

            // Error in "IN" clause
            originalFilter = "b in [1, 2]";
            ex = Assert.ThrowsException<ParserSyntaxException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("[1", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Count());
            Assert.IsTrue(ex.ExpectedTokens.Contains("("));

            // Error in "IS NULL" clause
            originalFilter = "b is not complex and should have been just called 'NULL'";
            ex = Assert.ThrowsException<ParserSyntaxException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("complex", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Count());
            Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            // Error in "IS NULL" clause
            originalFilter = "b is complex and should have been just called 'NULL'";
            ex = Assert.ThrowsException<ParserSyntaxException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("COMPLEX", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Count());
            Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            // Error in conjunctions between clauses
            originalFilter = "a = 'hi' BUTIREALLYTHINKTHAT b = 0";
            var ex2 = Assert.ThrowsException<ExpectedConjunctionException>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex2.OriginalFilter);
            Assert.AreEqual("BUTIREALLYTHINKTHAT", ex2.FoundToken);
        }


        [TestMethod]
        public void FilterParseTest()
        {
            var query = _source.Parse(null, "a = 'booya' AND b != 1", null);
            var sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("a = @p1 AND b <> @p2", sql.whereClause);
            Assert.AreEqual(2, sql.parameters.Count());
            Assert.AreEqual("booya", sql.parameters["@p1"]);
            Assert.AreEqual(1, sql.parameters["@p2"]);
        }

        [TestMethod]
        public void NullInWhereClause()
        {
            var query = _source.Parse(null, "a is null", null);
            var sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("a IS NULL", sql.whereClause);
            Assert.AreEqual(0, sql.parameters.Count);

            query = _source.Parse(null, "a is not null", null);
            sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("a IS NOT NULL", sql.whereClause);
            Assert.AreEqual(0, sql.parameters.Count);

            query = _source.Parse(null, "(  a  is  not  null )  or   ( a  is  null  )  ", null);
            sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("(a IS NOT NULL) OR (a IS NULL)", sql.whereClause);
            Assert.AreEqual(0, sql.parameters.Count);

            query = _source.Parse(null, "(((  a  is  not  null ))  or   ( a  is  null  ))  ", null);
            sql = SqlExecutor.RenderSQL(_source, query);
            Assert.AreEqual("(((a IS NOT NULL)) OR (a IS NULL))", sql.whereClause);
            Assert.AreEqual(0, sql.parameters.Count);
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