using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Query;
using Searchlight.Tests.Models;

// This file has lots of intentional misspellings
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable IdentifierTypo

// Highlighting allocations on this file is annoying 
// ReSharper disable HeapView.DelegateAllocation

namespace Searchlight.Tests.Executors
{
    [TestClass]
    public class LinqExecutorTests
    {
        private static List<EmployeeObj> _list;
        private static DataSource _src;
        private static Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _linq;
        
        [TestInitialize]
        public void SetupTests()
        {
            _list = EmployeeObj.GetTestList();
            _src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Strict);
            _linq = async tree => 
            {
                await Task.CompletedTask;
                return tree.QueryCollection(_list);
            };
        }

        [TestMethod]
        public async Task EmployeeTestSuite()
        {
            await Executors.EmployeeTestSuite.BasicTestSuite(_src, _list, _linq);
            await Executors.EmployeeTestSuite.CaseInsensitiveStringTestSuite(_src, _list, _linq);

            _src.Engine = new SearchlightEngine { StringComparison = StringComparison.OrdinalIgnoreCase };
            await Executors.EmployeeTestSuite.CaseInsensitiveStringTestSuite(_src, _list, _linq);
            
            _src.Engine = new SearchlightEngine { StringComparison = StringComparison.Ordinal };
            await Executors.EmployeeTestSuite.CaseSensitiveStringTestSuite(_src, _list, _linq);
        }
        
        // =========================================================
        // Below this line are LINQ specific tests
        // =========================================================

        [TestMethod]
        public void QueryPartiallyCompatibleCollection()
        {
            var list = CompatibleEmployeeObj.GetCompatibleList();

            // Try a few queries and sorts that _can_ work on a compatible type
            var syntax = _src.ParseFilter("name startswith c");
            var result = syntax.QueryCollection(list);
            Assert.AreEqual("Charlie Compatible", result.records[0].name);
            
            syntax = _src.Parse(new FetchRequest() { filter = "name startswith c", order = "onduty asc" });
            result = syntax.QueryCollection(list);
            Assert.AreEqual("Charlie Compatible", result.records[0].name);

            // Now try a query and a sort that won't work
            syntax = _src.ParseFilter("name startswith c and id = 57 and hired > 2020-02-01 and onduty = false");
            var ex = Assert.ThrowsException<FieldTypeMismatch>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("id on type CompatibleEmployeeObj", ex.FieldName);

            syntax = _src.Parse(new FetchRequest() { filter = "name startswith c", order = "id asc" });
            ex = Assert.ThrowsException<FieldTypeMismatch>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("id on type CompatibleEmployeeObj", ex.FieldName);
        }

        [TestMethod]
        public void QueryIncompatibleCollection()
        {
            var list = IncompatibleEmployeeObj.GetIncompatibleList();
            var syntax = _src.ParseFilter("name startswith a");
            syntax.PageSize = 1;
            syntax.PageNumber = 2;

            var ex = Assert.ThrowsException<FieldNotFound>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("name", ex.FieldName);
        }
    }
}