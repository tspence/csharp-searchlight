using NUnit.Framework;
using Searchlight.DataSource;
using Searchlight.Parsing;
using Searchlight.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Searchlight.Tests.Queries
{
    [TestFixture]
    public class QueryTests
    {
        public class EmployeeObj
        {
            public string name { get; set; }
            public int id { get; set; }
            public DateTime hired { get; set; }
            public decimal paycheck { get; set; }
            public bool onduty { get; set; }
        }

        public List<EmployeeObj> GetTestList()
        {
            List<EmployeeObj> list = new List<EmployeeObj>();
            list.Add(new EmployeeObj() { hired = DateTime.Today, id = 1, name = "Alice Smith", onduty = true, paycheck = 1000.00m });
            list.Add(new EmployeeObj() { hired = DateTime.Today.AddMonths(-1), id = 2, name = "Bob Rogers", onduty = true, paycheck = 1000.00m });
            list.Add(new EmployeeObj() { hired = DateTime.Today.AddMonths(-6), id = 3, name = "Charlie Prentiss", onduty = false, paycheck = 800.0m });
            list.Add(new EmployeeObj() { hired = DateTime.Today.AddMonths(-12), id = 4, name = "Danielle O'Shea", onduty = false, paycheck = 1200.0m });
            list.Add(new EmployeeObj() { hired = DateTime.Today.AddMonths(1), id = 5, name = "Ernest Nofzinger", onduty = true, paycheck = 1000.00m });
            return list;
        }

        [Test]
        public void SanityTest()
        {
            string[] companies = { "Consolidated Messenger", "Alpine Ski House", "Southridge Video", "City Power & Light",
                               "Coho Winery", "Wide World Importers", "Graphic Design Institute", "Adventure Works",
                               "Humongous Insurance", "Woodgrove Bank", "Margie's Travel", "Northwind Traders",
                               "Blue Yonder Airlines", "Trey Research", "The Phone Company",
                               "Wingtip Toys", "Lucerne Publishing", "Fourth Coffee" };

            // The IQueryable data to query.
            IQueryable<String> queryableData = companies.AsQueryable<string>();

            // Compose the expression tree that represents the parameter to the predicate.
            ParameterExpression pe = Expression.Parameter(typeof(string), "company");

            // ***** Where(company => (company.ToLower() == "coho winery" || company.Length > 16)) *****
            // Create an expression tree that represents the expression 'company.ToLower() == "coho winery"'.
            Expression left = Expression.Call(pe, typeof(string).GetMethod("ToLower", System.Type.EmptyTypes));
            Expression right = Expression.Constant("coho winery");
            Expression e1 = Expression.Equal(left, right);

            // Create an expression tree that represents the expression 'company.Length > 16'.
            left = Expression.Property(pe, typeof(string).GetProperty("Length"));
            right = Expression.Constant(16, typeof(int));
            Expression e2 = Expression.GreaterThan(left, right);

            // Combine the expression trees to create an expression tree that represents the 
            // expression '(company.ToLower() == "coho winery" || company.Length > 16)'.
            Expression predicateBody = Expression.OrElse(e1, e2);

            // Create an expression tree that represents the expression 
            // 'queryableData.Where(company => (company.ToLower() == "coho winery" || company.Length > 16))'
            MethodCallExpression whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { queryableData.ElementType },
                queryableData.Expression,
                Expression.Lambda<Func<string, bool>>(predicateBody, new ParameterExpression[] { pe }));
            // ***** End Where ***** 

            // ***** OrderBy(company => company) ***** 
            // Create an expression tree that represents the expression 
            // 'whereCallExpression.OrderBy(company => company)'
            MethodCallExpression orderByCallExpression = Expression.Call(
                typeof(Queryable),
                "OrderBy",
                new Type[] { queryableData.ElementType, queryableData.ElementType },
                whereCallExpression,
                Expression.Lambda<Func<string, string>>(pe, new ParameterExpression[] { pe }));
            // ***** End OrderBy ***** 

            // Create an executable query from the expression tree.
            IQueryable<string> results = queryableData.Provider.CreateQuery<string>(orderByCallExpression);

            // Enumerate the results. 
            foreach (string company in results)
                Console.WriteLine(company);
        }

        [Test]
        public void QueryListCollection()
        {
            var list = GetTestList();

            // Construct a simple query and check that it comes out correct
            SearchlightDataSource src = SearchlightDataSource.FromCollection(list);
            var query = SafeQueryParser.ParseFilter("id gt 1 and paycheck le 1000", src);
            Assert.AreEqual(2, query.Count());
            Assert.AreEqual(ConjunctionType.AND, query[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause)query[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)query[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause)query[0]).Value);
            Assert.AreEqual("paycheck", ((CriteriaClause)query[1]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause)query[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)query[1]).Value);

            // Execute the query and ensure that each result matches
            var results = SafeQuery.QueryCollection<EmployeeObj>(src, query, list);
            Assert.True(results.Count() == 3);
            foreach (var e in results) {
                Assert.True(e.id > 1);
                Assert.True(e.paycheck <= 1000.0m);
            }
        }
    }
}
