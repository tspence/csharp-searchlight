using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Configuration.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Tests
{
    [TestClass]
    public class ExceptionTests
    {
        public SearchlightDataSource getSource()
        {
            var safeColumns = new CustomColumnDefinition()
               .WithColumn("a", typeof(String), null)
               .WithColumn("b", typeof(Int32), null)
               .WithColumn("colLong", typeof(Int64), null)
               .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
               .WithColumn("colULong", typeof(UInt64), null)
               .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
               .WithColumn("colGuid", typeof(Guid), null);

            var src = new SearchlightDataSource();
            src.ColumnDefinitions = safeColumns;
            src.MaximumParameters = 200;
            src.DefaultSortField = "a";
            return src;
        }


        [TestMethod]
        public void EmptyClauseTest()
        {
            var src = getSource();
            var originalFilter = "((()))";
            var ex = Assert.ThrowsException<EmptyClause>((Action)(() =>
            {
                var query = src.Parse(originalFilter);
                var sql = SqlExecutor.RenderSQL(src, query);
            }));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }
    }
}
