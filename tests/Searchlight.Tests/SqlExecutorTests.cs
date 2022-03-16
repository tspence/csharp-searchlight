using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Searchlight.Tests
{
    [TestClass]
    public class SqlExecutorTests
    {
        private readonly DataSource _source;
        private readonly SearchlightEngine _engine;

        public SqlExecutorTests()
        {
            _source = new DataSource()
                .WithColumn("a", typeof(String))
                .WithColumn("b", typeof(Int32))
                .WithColumn("colLong", typeof(Int64))
                .WithColumn("colNullableGuid", typeof(Nullable<Guid>))
                .WithColumn("colULong", typeof(UInt64))
                .WithColumn("colNullableULong", typeof(Nullable<UInt64>))
                .WithColumn("colGuid", typeof(Guid));
            _source.MaximumParameters = 200;
            _source.DefaultSort = "a";
            _source.TableName = "MyTable";
            _engine = new SearchlightEngine()
                .AddDataSource(_source);
            _engine.useResultSet = true;
        }

        [TestMethod]
        public void TooManyParameters()
        {
            string originalFilter =
                "b in (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256)";
            var ex = Assert.ThrowsException<TooManyParameters>(() =>
            {
                var query = _source.Parse(originalFilter);
                query.ToSqlServerCommand();
            });
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Verify that 0 is treated as unlimited
            _source.MaximumParameters = 0;
            var syntax = _source.Parse(originalFilter);
            Assert.IsNotNull(syntax);
            _source.MaximumParameters = null;

            // Verify that we can also set maximum parameters at the engine level
            _source.Engine = new SearchlightEngine
            {
                MaximumParameters = 50
            };
            ex = Assert.ThrowsException<TooManyParameters>(() =>
            {
                var query = _source.Parse(originalFilter);
                query.ToSqlServerCommand();
            });
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void CaseSensitivity()
        {
            // First test, lowercase
            var query = _source.Parse("b between 1 and 5");
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.WhereClause.ToString());
            Assert.AreEqual(2, sql.Parameters.Count);

            // Second test, proper case
            query = _source.Parse("b Between 1 And 5");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.WhereClause.ToString());
            Assert.AreEqual(2, sql.Parameters.Count);

            // Third test, uppercase
            query = _source.Parse("b BETWEEN 1 AND 5");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("b BETWEEN @p1 AND @p2", sql.WhereClause.ToString());
            Assert.AreEqual(2, sql.Parameters.Count);
            
            // Fourth test, inverse
            query = _source.Parse("b not BETWEEN 1 AND 5");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("b NOT BETWEEN @p1 AND @p2", sql.WhereClause.ToString());
            Assert.AreEqual(2, sql.Parameters.Count);
        }

        [TestMethod]
        public void CommonSyntaxErrors()
        {
            // Error in "IN" clause
            string originalFilter = "b in (1, 2>*#]";
            var ex = Assert.ThrowsException<InvalidToken>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual(">", ex.BadToken);
            Assert.AreEqual(2, ex.ExpectedTokens.Length);
            Assert.IsTrue(ex.ExpectedTokens.Contains(","));
            Assert.IsTrue(ex.ExpectedTokens.Contains(")"));

            // Error in "IN" clause
            originalFilter = "b in [1, 2]";
            ex = Assert.ThrowsException<InvalidToken>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("[1", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Length);
            Assert.IsTrue(ex.ExpectedTokens.Contains("("));

            // Error in "IS NULL" clause
            originalFilter = "b is not complex and should have been just called 'NULL'";
            ex = Assert.ThrowsException<InvalidToken>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("complex", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Length);
            Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            // Error in "IS NULL" clause
            originalFilter = "b is complex and should have been just called 'NULL'";
            ex = Assert.ThrowsException<InvalidToken>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
            Assert.AreEqual("COMPLEX", ex.BadToken);
            Assert.AreEqual(1, ex.ExpectedTokens.Length);
            Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            // Error in conjunctions between clauses
            originalFilter = "a = 'hi' BUTIREALLYTHINKTHAT b = 0";
            var ex2 = Assert.ThrowsException<InvalidToken>(() => _source.ParseFilter(originalFilter));
            Assert.AreEqual(originalFilter, ex2.OriginalFilter);
            Assert.AreEqual("BUTIREALLYTHINKTHAT", ex2.BadToken);
            Assert.AreEqual(5, ex2.ExpectedTokens.Length);
            Assert.AreEqual("(", ex2.ExpectedTokens[0]);
            Assert.AreEqual(")", ex2.ExpectedTokens[1]);
            Assert.AreEqual("AND", ex2.ExpectedTokens[2]);
            Assert.AreEqual("OR", ex2.ExpectedTokens[3]);
            Assert.AreEqual("NOT", ex2.ExpectedTokens[4]);
        }


        [TestMethod]
        public void FilterParseTest()
        {
            var query = _source.Parse("a = 'booya' AND b != 1");
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("a = @p1 AND b <> @p2", sql.WhereClause.ToString());
            Assert.AreEqual(2, sql.Parameters.Count);
            Assert.AreEqual("booya", sql.Parameters["@p1"]);
            Assert.AreEqual(1, sql.Parameters["@p2"]);
        }

        [TestMethod]
        public void NullInWhereClause()
        {
            var query = _source.Parse("a is null");
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("a IS NULL", sql.WhereClause.ToString());
            Assert.AreEqual(0, sql.Parameters.Count);

            query = _source.Parse("a is not null");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("a IS NOT NULL", sql.WhereClause.ToString());
            Assert.AreEqual(0, sql.Parameters.Count);

            query = _source.Parse("(  a  is  not  null )  or   ( a  is  null  )  ");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("(a IS NOT NULL) OR (a IS NULL)", sql.WhereClause.ToString());
            Assert.AreEqual(0, sql.Parameters.Count);

            query = _source.Parse("(((  a  is  not  null ))  or   ( a  is  null  ))  ");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("(((a IS NOT NULL)) OR (a IS NULL))", sql.WhereClause.ToString());
            Assert.AreEqual(0, sql.Parameters.Count);
        }

        public string ParseWhereClause(string filter)
        {
            var query = _source.Parse(filter);
            var sql = query.ToSqlServerCommand();
            return sql.WhereClause.ToString();
        }

        [TestMethod]
        public void AllQueryExpressions()
        {
            // Try all basic query expression types - should succeed
            Assert.AreEqual("a = @p1", ParseWhereClause("a = 'test'"));
            Assert.AreEqual("a = @p1", ParseWhereClause("a eq 'test'"));
            Assert.AreEqual("a > @p1", ParseWhereClause("a > 'test'"));
            Assert.AreEqual("a > @p1", ParseWhereClause("a gt 'test'"));
            Assert.AreEqual("a >= @p1", ParseWhereClause("a >= 'test'"));
            Assert.AreEqual("a >= @p1", ParseWhereClause("a ge 'test'"));
            Assert.AreEqual("a <> @p1", ParseWhereClause("a <> 'test'"));
            Assert.AreEqual("a <> @p1", ParseWhereClause("a != 'test'"));
            Assert.AreEqual("a <> @p1", ParseWhereClause("a ne 'test'"));
            Assert.AreEqual("a < @p1", ParseWhereClause("a < 'test'"));
            Assert.AreEqual("a < @p1", ParseWhereClause("a lt 'test'"));
            Assert.AreEqual("a <= @p1", ParseWhereClause("a <= 'test'"));
            Assert.AreEqual("a <= @p1", ParseWhereClause("a le 'test'"));

            // Try slightly more complex query expression types - should succeed
            Assert.AreEqual("a BETWEEN @p1 AND @p2", ParseWhereClause("a between 'test1' and 'test9'"));
            Assert.AreEqual("a IN (@p1, @p2)", ParseWhereClause("a in ('test', 'test2')"));
            Assert.AreEqual("a LIKE @p1", ParseWhereClause("a startswith 'test%'"));
            Assert.AreEqual("a LIKE @p1", ParseWhereClause("a endswith 'test'"));
            Assert.AreEqual("a LIKE @p1", ParseWhereClause("a contains 'test'"));
            Assert.AreEqual("a NOT LIKE @p1", ParseWhereClause("a not startswith 'test%'"));
            Assert.AreEqual("a NOT LIKE @p1", ParseWhereClause("a not endswith 'test'"));
            Assert.AreEqual("a NOT LIKE @p1", ParseWhereClause("a not contains 'test'"));
            Assert.AreEqual("a IS NULL", ParseWhereClause("a is null"));
            Assert.AreEqual("a IS NOT NULL", ParseWhereClause("a is not null"));
            Assert.AreEqual("a NOT IN (@p1, @p2)", ParseWhereClause("a not in ('test', 'test2')"));

            // Now try some that fail
            Assert.ThrowsException<InvalidToken>(() => ParseWhereClause("a REALLYSHOULDBE 'test'"));
            Assert.ThrowsException<InvalidToken>(() => ParseWhereClause("a !<= 'test'"));
        }

        [TestMethod]
        public void TypeComparisons()
        {
            // Test the Int64
            var query = _source.Parse("collong eq 123456789123456");
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("colLong = @p1", sql.WhereClause.ToString());
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);

            // Test the guid
            query = _source.Parse(String.Format("colguid eq '{0}'", Guid.Empty.ToString()));
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("colGuid = @p1", sql.WhereClause.ToString());
            Assert.AreEqual(Guid.Empty, sql.Parameters["@p1"]);

            // Test the nullable guid
            query = _source.Parse(String.Format("colNullableGuid is null or colNullableGuid = '{0}'",
                Guid.Empty.ToString()));
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("colNullableGuid IS NULL OR colNullableGuid = @p1", sql.WhereClause.ToString());
            Assert.AreEqual(Guid.Empty, sql.Parameters["@p1"]);

            // Test the ULONG and nullable ULONG
            query = _source.Parse("colULong > 12345 or colNullableULong = 6789456");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("colULong > @p1 OR colNullableULong = @p2", sql.WhereClause.ToString());
            Assert.AreEqual(12345UL, sql.Parameters["@p1"]);
            Assert.AreEqual(6789456UL, sql.Parameters["@p2"]);

            // Test the ULONG and nullable ULONG when compared to a boolean - necessary for redshift
            query = _source.Parse("colULong = true OR colULong = false");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("colULong = @p1 OR colULong = @p2", sql.WhereClause.ToString());
            Assert.AreEqual(1UL, sql.Parameters["@p1"]);
            Assert.AreEqual(0UL, sql.Parameters["@p2"]);

            // Nullable variant
            query = _source.Parse("colNullableULong = true OR colNullableULong = false");
            sql = query.ToSqlServerCommand();
            Assert.AreEqual("colNullableULong = @p1 OR colNullableULong = @p2", sql.WhereClause.ToString());
            Assert.AreEqual(1UL, sql.Parameters["@p1"]);
            Assert.AreEqual(0UL, sql.Parameters["@p2"]);
        }

        [TestMethod]
        public void SqlServerBasicQuery()
        {
            // Basic query including where and order
            var query = _source.Parse("collong eq 123456789123456", null, "b ascending");
            _engine.useResultSet = false;
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET NOCOUNT ON;\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                            "SELECT * FROM MyTable WHERE colLong = @p1 ORDER BY b ASC", sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
        }

        [TestMethod]
        public void SqlServerPaginated()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong eq 123456789123456", order = "b desc", pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            _engine.useResultSet = false;
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual(
                "SET NOCOUNT ON;\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                "SELECT * FROM MyTable WHERE colLong = @p1 ORDER BY b DESC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
        }

        [TestMethod]
        public void SqlServerMultiFetch()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong eq 123456789123456", order = "b desc", pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET NOCOUNT ON;\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                            "SELECT COUNT(1) AS TotalRecords FROM MyTable WHERE colLong = @p1;\n" +
                            "SELECT * FROM MyTable WHERE colLong = @p1 ORDER BY b DESC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
        }

        [TestMethod]
        public void SqlServerMultiFetchDefaultSort()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong eq 123456789123456", order = null, pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET NOCOUNT ON;\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                            "SELECT COUNT(1) AS TotalRecords FROM MyTable WHERE colLong = @p1;\n" +
                            "SELECT * FROM MyTable WHERE colLong = @p1 ORDER BY a ASC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
        }

        [TestMethod]
        public void MultipleNestedClausesSql()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong gt 123456789123456 AND collong lt 987654321 AND (a eq 'Alice' or a eq 'Bob' or a eq 'Charlie') AND b < 10", order = null, pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET NOCOUNT ON;\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                            "SELECT COUNT(1) AS TotalRecords FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6;\n" +
                            "SELECT * FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6 ORDER BY a ASC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
            Assert.AreEqual((long)987654321, sql.Parameters["@p2"]);
            Assert.AreEqual("Alice", sql.Parameters["@p3"]);
            Assert.AreEqual("Bob", sql.Parameters["@p4"]);
            Assert.AreEqual("Charlie", sql.Parameters["@p5"]);
            Assert.AreEqual(10, sql.Parameters["@p6"]);
        }

        [TestMethod]
        public void TestNoIntro()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong gt 123456789123456 AND collong lt 987654321 AND (a eq 'Alice' or a eq 'Bob' or a eq 'Charlie') AND b < 10", order = null, pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            _engine.useNoLock = true;
            _engine.useReadUncommitted = false;
            _engine.useNoCount = false;
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SELECT COUNT(1) AS TotalRecords FROM MyTable WITH (nolock) WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6;\n" +
                            "SELECT * FROM MyTable WITH (nolock) WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6 ORDER BY a ASC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
            Assert.AreEqual((long)987654321, sql.Parameters["@p2"]);
            Assert.AreEqual("Alice", sql.Parameters["@p3"]);
            Assert.AreEqual("Bob", sql.Parameters["@p4"]);
            Assert.AreEqual("Charlie", sql.Parameters["@p5"]);
            Assert.AreEqual(10, sql.Parameters["@p6"]);
        }

        [TestMethod]
        public void TestNocount()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong gt 123456789123456 AND collong lt 987654321 AND (a eq 'Alice' or a eq 'Bob' or a eq 'Charlie') AND b < 10", order = null, pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            _engine.useNoCount = false;
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\n" +
                            "SELECT COUNT(1) AS TotalRecords FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6;\n" +
                            "SELECT * FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6 ORDER BY a ASC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
            Assert.AreEqual((long)987654321, sql.Parameters["@p2"]);
            Assert.AreEqual("Alice", sql.Parameters["@p3"]);
            Assert.AreEqual("Bob", sql.Parameters["@p4"]);
            Assert.AreEqual("Charlie", sql.Parameters["@p5"]);
            Assert.AreEqual(10, sql.Parameters["@p6"]);
        }

        [TestMethod]
        public void TestReadUncommitted()
        {
            // Fetch request including pagination
            var fetch = new FetchRequest()
                {filter = "collong gt 123456789123456 AND collong lt 987654321 AND (a eq 'Alice' or a eq 'Bob' or a eq 'Charlie') AND b < 10", order = null, pageNumber = 2, pageSize = 50};
            var query = _source.Parse(fetch);
            _engine.useReadUncommitted = false;
            var sql = query.ToSqlServerCommand();
            Assert.AreEqual("SET NOCOUNT ON;\n" +
                            "SELECT COUNT(1) AS TotalRecords FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6;\n" +
                            "SELECT * FROM MyTable WHERE colLong > @p1 AND colLong < @p2 AND (a = @p3 OR a = @p4 OR a = @p5) AND b < @p6 ORDER BY a ASC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY;\n",
                sql.CommandText);
            Assert.AreEqual(123456789123456, sql.Parameters["@p1"]);
            Assert.AreEqual((long)987654321, sql.Parameters["@p2"]);
            Assert.AreEqual("Alice", sql.Parameters["@p3"]);
            Assert.AreEqual("Bob", sql.Parameters["@p4"]);
            Assert.AreEqual("Charlie", sql.Parameters["@p5"]);
            Assert.AreEqual(10, sql.Parameters["@p6"]);
        }
    }
}