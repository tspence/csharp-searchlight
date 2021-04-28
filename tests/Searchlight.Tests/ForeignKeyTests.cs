using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Query;

namespace Searchlight.Tests
{
    [TestClass]
    public class ForeignKeyTests
    {
        [SearchlightModel]
        public class LibraryBook
        {
            [SearchlightField]
            // ReSharper disable once InconsistentNaming
            public string ISBN { get; set; }

            [SearchlightField] public string Name { get; set; }
            [SearchlightField] public string Author { get; set; }

            [SearchlightCollection(KeyName = "ISBN")]
            public BookReservation[] WaitList { get; set; }

            [SearchlightCollection(KeyName = "ISBN")]
            public BookCopy[] Copies { get; set; }
        }

        [SearchlightModel]
        public class BookReservation
        {
            [SearchlightField]
            // ReSharper disable once InconsistentNaming
            public string ISBN { get; set; }

            [SearchlightField] public DateTime DateRequested { get; set; }
            [SearchlightField] public string CustomerName { get; set; }
        }

        [SearchlightModel(OriginalName = "BookCopies")]
        public class BookCopy
        {
            [SearchlightField]
            // ReSharper disable once InconsistentNaming
            public string ISBN { get; set; }

            [SearchlightField] public bool CheckedOut { get; set; }
            [SearchlightField] public DateTime DateCheckedOut { get; set; }
            [SearchlightField] public DateTime DateReturned { get; set; }
            [SearchlightField] public string CustomerName { get; set; }
        }

        [TestMethod]
        public void TestBasicForeignKey()
        {
            var engine = new SearchlightEngine()
                .AddClass(typeof(LibraryBook))
                .AddClass(typeof(BookReservation));

            // Let's try a complex request on multiple tables 
            var request = new FetchRequest()
            {
                table = "LibraryBook",
                filter = "Author startsWith 'Alice'",
                order = "Name ASC",
                include = "WaitList",
                pageNumber = 1,
                pageSize = 20,
            };
            SyntaxTree syntax = engine.Parse(request);

            // Convert this into a multiple recordset SQL string
            var query = syntax.ToSqlServerCommand(true);
            Assert.AreEqual("SELECT COUNT(1) AS TotalRecords FROM LibraryBook WHERE Author LIKE @p1;\n" +
                            "SELECT * INTO #temp FROM LibraryBook WHERE Author LIKE @p1 ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;\n" +
                            "SELECT * FROM #temp ORDER BY Name ASC;\n" +
                            "SELECT * FROM BookReservation t1 INNER JOIN #temp ON t1.ISBN = #temp.ISBN;\n" +
                            "DROP TABLE #temp;\n", query.CommandText);
        }

        [TestMethod]
        public void TestMultipleCollections()
        {
            var engine = new SearchlightEngine()
                .AddClass(typeof(LibraryBook))
                .AddClass(typeof(BookReservation))
                .AddClass(typeof(BookCopy));

            // Let's try a complex request on multiple tables 
            var request = new FetchRequest()
            {
                table = "LibraryBook",
                filter = "Author startsWith 'Alice'",
                order = "Name ASC",
                include = "WaitList, Copies",
                pageNumber = 1,
                pageSize = 20,
            };
            SyntaxTree syntax = engine.Parse(request);

            // Convert this into a multiple recordset SQL string
            var query = syntax.ToSqlServerCommand(true);
            Assert.AreEqual("SELECT COUNT(1) AS TotalRecords FROM LibraryBook WHERE Author LIKE @p1;\n" +
                            "SELECT * INTO #temp FROM LibraryBook WHERE Author LIKE @p1 ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;\n" +
                            "SELECT * FROM #temp ORDER BY Name ASC;\n" +
                            "SELECT * FROM BookReservation t1 INNER JOIN #temp ON t1.ISBN = #temp.ISBN;\n" +
                            "SELECT * FROM BookCopies t2 INNER JOIN #temp ON t2.ISBN = #temp.ISBN;\n" +
                            "DROP TABLE #temp;\n", query.CommandText);
        }
    }
}