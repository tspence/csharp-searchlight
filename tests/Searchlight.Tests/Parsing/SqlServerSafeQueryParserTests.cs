using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Configuration.Default;
using Searchlight.DataSource;
using Searchlight.Exceptions;
using Searchlight.Parsing;
using Searchlight.Executors;
using System;
using System.Linq;

namespace Searchlight.Tests.Parsing
{
    [TestClass]
    public class SqlServerSafeQueryParserTests
    {
        private SearchlightDataSource _source;

        public SqlServerSafeQueryParserTests()
        {
               var safeColumns = new CustomColumnDefinition()
                   .WithColumn("a", typeof(String), null)
                   .WithColumn("b", typeof(Int32), null);

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
            var ex = Assert.ThrowsException<TooManyParametersException>(() => {
                var query = _source.Parse(null, originalFilter, null);
                var sql = SqlExecutor.RenderSQL(_source, query);
            });
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod("Parser.CaseSensitivity")]
        public void CaseSensitivity()
        {
            // // First test, lowercase
            // string originalFilter = "b between 1 and 5";
            // var ok = _parser.ParseFilter(originalFilter);
            // Assert.AreEqual("b BETWEEN @p1 AND @p2", ok.ValidatedFilter);
            // Assert.AreEqual(2, ok.SqlParameters.ParameterNames.Count());

            // // Second test, proper case
            // originalFilter = "b Between 1 And 5";
            // ok = _parser.ParseFilter(originalFilter);
            // Assert.AreEqual("b BETWEEN @p1 AND @p2", ok.ValidatedFilter);
            // Assert.AreEqual(2, ok.SqlParameters.ParameterNames.Count());

            // // Third test, uppercase
            // originalFilter = "b BETWEEN 1 AND 5";
            // ok = _parser.ParseFilter(originalFilter);
            // Assert.AreEqual("b BETWEEN @p1 AND @p2", ok.ValidatedFilter);
            // Assert.AreEqual(2, ok.SqlParameters.ParameterNames.Count());
        }

        [TestMethod("Parser.CommonSyntaxErrors")]
        public void CommonSyntaxErrors()
        {
            //// Error in "IN" clause
            //string originalFilter = "b in (1, 2>*#]";
            //var ex = Assert.ThrowsException<ParserSyntaxException>(() => _parser.ParseWhereClause(originalFilter));
            //Assert.AreEqual(originalFilter, ex.OriginalFilter);
            //Assert.AreEqual(">", ex.BadToken);
            //Assert.AreEqual(2, ex.ExpectedTokens.Count());
            //Assert.IsTrue(ex.ExpectedTokens.Contains(","));
            //Assert.IsTrue(ex.ExpectedTokens.Contains(")"));

            //// Error in "IN" clause
            //originalFilter = "b in [1, 2]";
            //ex = Assert.ThrowsException<ParserSyntaxException>(() => _parser.ParseWhereClause(originalFilter));
            //Assert.AreEqual(originalFilter, ex.OriginalFilter);
            //Assert.AreEqual("[1", ex.BadToken);
            //Assert.AreEqual(1, ex.ExpectedTokens.Count());
            //Assert.IsTrue(ex.ExpectedTokens.Contains("("));

            //// Error in "IS NULL" clause
            //originalFilter = "b is not complex and should have been just called 'NULL'";
            //ex = Assert.ThrowsException<ParserSyntaxException>(() => _parser.ParseWhereClause(originalFilter));
            //Assert.AreEqual(originalFilter, ex.OriginalFilter);
            //Assert.AreEqual("complex", ex.BadToken);
            //Assert.AreEqual(1, ex.ExpectedTokens.Count());
            //Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            //// Error in "IS NULL" clause
            //originalFilter = "b is complex and should have been just called 'NULL'";
            //ex = Assert.ThrowsException<ParserSyntaxException>(() => _parser.ParseWhereClause(originalFilter));
            //Assert.AreEqual(originalFilter, ex.OriginalFilter);
            //Assert.AreEqual("complex", ex.BadToken);
            //Assert.AreEqual(1, ex.ExpectedTokens.Count());
            //Assert.IsTrue(ex.ExpectedTokens.Contains("NULL"));

            //// Error in conjunctions between clauses
            //originalFilter = "a = 'hi' BUTIREALLYTHINKTHAT b = 0";
            //ex = Assert.ThrowsException<ParserSyntaxException>(() => _parser.ParseWhereClause(originalFilter));
            //Assert.AreEqual(originalFilter, ex.OriginalFilter);
            //Assert.AreEqual("BUTIREALLYTHINKTHAT", ex.BadToken);
            //Assert.AreEqual(5, ex.ExpectedTokens.Count());
            //Assert.IsTrue(ex.ExpectedTokens.Contains("AND"));
            //Assert.IsTrue(ex.ExpectedTokens.Contains("OR"));
        }
    }
}