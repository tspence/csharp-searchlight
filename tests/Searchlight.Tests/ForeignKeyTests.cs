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
            [SearchlightField]
            public string Name { get; set; }
            [SearchlightField]
            public string Author { get; set; }
            [SearchlightCollection(ForeignTableName = "BookReservation", LocalKey= "ISBN", ForeignTableKey = "ISBN")]
            public BookReservation[] WaitList { get; set; }
        }

        [SearchlightModel]
        public class BookReservation
        {
            [SearchlightField]
            // ReSharper disable once InconsistentNaming
            public string ISBN { get; set; }
            [SearchlightField]
            public DateTime DateRequested { get; set; }
            [SearchlightField]
            public string CustomerName { get; set; }
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
            Assert.AreEqual("SELECT * INTO #temp FROM LibraryBook WHERE Author LIKE @p1;\n" +
                "SELECT COUNT(1) AS TotalRecords FROM #temp;\n" +
                "SELECT * FROM #temp ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;\n" +
                "SELECT * FROM BookReservation INNER JOIN #temp ON #temp.ISBN = BookReservation.ISBN;\n" +
                "DROP TABLE #temp;\n", query.CommandText);
        }
    }
}