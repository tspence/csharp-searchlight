using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Searchlight.Tests
{
    [TestClass]
    public class ExceptionTests
    {
        public DataSource getSource()
        {
            var src = new DataSource()
               .WithColumn("a", typeof(String))
               .WithColumn("b", typeof(Int32))
               .WithColumn("colLong", typeof(Int64))
               .WithColumn("colNullableGuid", typeof(Nullable<Guid>))
               .WithColumn("colULong", typeof(UInt64))
               .WithColumn("colNullableULong", typeof(Nullable<UInt64>))
               .WithColumn("colGuid", typeof(Guid));
            src.MaximumParameters = 200;
            src.DefaultSort = "a";
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
                var sql = query.ToSqlServerCommand(false);
            }));
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }
    }
}
