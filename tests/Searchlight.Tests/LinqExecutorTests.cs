using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

// This file has lots of intentional misspellings
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable IdentifierTypo

// Highlighting allocations on this file is annoying 
// ReSharper disable HeapView.DelegateAllocation

namespace Searchlight.Tests
{
    [TestClass]
    public class LinqExecutorTests
    {
        private readonly DataSource _src;
        
        [SearchlightModel(DefaultSort = nameof(name))]
        public class EmployeeObj
        {
            public string name { get; set; }
            public int id { get; set; }
            public DateTime hired { get; set; }
            public decimal paycheck { get; set; }
            public bool onduty { get; set; }
        }

        public class CompatibleEmployeeObj
        {
            public string name { get; set; }
            public int? id { get; set; }
            public string hired { get; set; }
            public string paycheck { get; set; }
            public bool onduty { get; set; }
        }

        public class IncompatibleEmployeeObj
        {
            public string FullName { get; set; }
            public int Identifier { get; set; }
        }

        private static List<IncompatibleEmployeeObj> GetIncompatibleList()
        {
            return new List<IncompatibleEmployeeObj>()
            {
                new IncompatibleEmployeeObj()
                {
                    FullName = "Irving Incompatible",
                    Identifier = 1,
                },
                new IncompatibleEmployeeObj()
                {
                    FullName = "Noreen Negative",
                    Identifier = -1,
                }
            };
        }

        private static List<CompatibleEmployeeObj> GetCompatibleList()
        {
            return new List<CompatibleEmployeeObj>()
            {
                new CompatibleEmployeeObj()
                {
                    name = "Charlie Compatible",
                    id = 57,
                    hired = "true",
                    paycheck = "$1000.00",
                    onduty = false
                },
                new CompatibleEmployeeObj()
                {
                    name = "Nelly Null",
                    id = null,
                    hired = null,
                    paycheck = null,
                    onduty = false
                },
            };
        }

        private static List<EmployeeObj> GetTestList()
        {
            return new List<EmployeeObj>
            {
                new()
                    { hired = DateTime.Today, id = 1, name = "Alice Smith", onduty = true, paycheck = 1000.00m },
                new()
                {
                    hired = DateTime.Today.AddMonths(-1),
                    id = 2,
                    name = "Bob Rogers",
                    onduty = true,
                    paycheck = 1000.00m
                },
                new()
                {
                    hired = DateTime.Today.AddMonths(-6),
                    id = 3,
                    name = "Charlie Prentiss",
                    onduty = false,
                    paycheck = 800.0m
                },
                new()
                {
                    hired = DateTime.Today.AddMonths(-12),
                    id = 4,
                    name = "Danielle O'Shea",
                    onduty = false,
                    paycheck = 1200.0m
                },
                new()
                {
                    hired = DateTime.Today.AddMonths(1),
                    id = 5,
                    name = "Ernest Nofzinger",
                    onduty = true,
                    paycheck = 1000.00m
                },
                new()
                    { hired = DateTime.Today.AddMonths(4), id = 6, name = null, onduty = false, paycheck = 10.00m },
                new()
                {
                    hired = DateTime.Today.AddMonths(2),
                    id = 7,
                    name = "Roderick 'null' Sqlkeywordtest",
                    onduty = false,
                    paycheck = 578.00m
                },
                new()
                {
                    hired = DateTime.UtcNow.AddHours(-1),
                    id = 8,
                    name = "Joe 'Fresh Hire' McGillicuddy",
                    onduty = false,
                    paycheck = 123.00m,
                },
                new()
                {
                    hired = DateTime.UtcNow.AddHours(1),
                    id = 8,
                    name = "Carol 'Starting Soon!' Yamashita",
                    onduty = false,
                    paycheck = 987.00m,
                }
            };
        }

        public LinqExecutorTests()
        {
            this._src = DataSource.Create(null, typeof(EmployeeObj), AttributeMode.Loose);
        }

        [TestMethod]
        public void QueryListCollection()
        {
            var list = GetTestList();

            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and paycheck le 1000");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause) syntax.Filter[0]).Value);
            Assert.AreEqual("paycheck", ((CriteriaClause) syntax.Filter[1]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause) syntax.Filter[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause) syntax.Filter[1]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck <= 1000.0m);
            }
        }


        [TestMethod]
        public void NestedClauseQuery()
        {
            var list = GetTestList();

            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and (paycheck lt 1000 or paycheck gt 1000)");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause) syntax.Filter[0]).Value);

            // Did we get a nested clause?
            var cc = syntax.Filter[1] as CompoundClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual(2, cc.Children.Count);
            Assert.AreEqual("paycheck", ((CriteriaClause) cc.Children[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause) cc.Children[0]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause) cc.Children[0]).Value);
            Assert.AreEqual("paycheck", ((CriteriaClause) cc.Children[1]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause) cc.Children[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause) cc.Children[1]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(6, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck is 800.0m or 1200.0m or 10.0m or 578.00m or 123.00m or 987.00m);
            }
        }

        [TestMethod]
        public void BetweenQuery()
        {
            var list = GetTestList();

            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("id between 2 and 4");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(false, syntax.Filter[0].Negated);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((BetweenClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause) syntax.Filter[0]).LowerValue);
            Assert.AreEqual(4, ((BetweenClause) syntax.Filter[0]).UpperValue);
            
            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(3, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.id < 5);
            }

            // Test the opposite
            syntax = _src.Parse("id not between 2 and 4");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(true, syntax.Filter[0].Negated);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((BetweenClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause) syntax.Filter[0]).LowerValue);
            Assert.AreEqual(4, ((BetweenClause) syntax.Filter[0]).UpperValue);
            results = syntax.QueryCollection(list);
            Assert.AreEqual(6, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id is <= 1 or >= 5);
            }
        }


        [TestMethod]
        public void StartsWithQuery()
        {
            var list = GetTestList();

            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name startswith 'A'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.StartsWith, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("A", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name[0] == 'A');
            }
        }


        [TestMethod]
        public void EndsWithQuery()
        {
            var list = GetTestList();

            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name endswith 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.EndsWith, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(2, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name.EndsWith("s", StringComparison.OrdinalIgnoreCase));
            }
        }


        [TestMethod]
        public void ContainsQuery()
        {
            var list = GetTestList();

            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name contains 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.Contains, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause) syntax.Filter[0]).Value);
            
            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(8, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e != null && e.name.Contains('s', StringComparison.OrdinalIgnoreCase));
            }
            
            // Now test the opposite
            syntax = _src.Parse("name not contains 's'");
            results = syntax.QueryCollection(list);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e != null && (e.name == null || !e.name.Contains('s', StringComparison.OrdinalIgnoreCase)));
            }
        }
        
        [TestMethod]
        public void GreaterThanQuery()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("name gt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) > 0);
            }
        }

        [TestMethod]
        public void GreaterThanOrEqualQuery()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("name ge 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThanOrEqual, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers", StringComparison.CurrentCultureIgnoreCase) >= 0);
            }
        }
        
        [TestMethod]
        public void LessThanQuery()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("name lt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) < 0);
            }
        }
        
        [TestMethod]
        public void LessThanOrEqualQuery()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("name le 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause) syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause) syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause) syntax.Filter[0]).Value);

            // Execute the query and ensure that each result matches
            var results = syntax.QueryCollection(list);
            Assert.AreEqual(2, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers", StringComparison.CurrentCultureIgnoreCase) <= 0);
            }
        }

        [TestMethod]
        public void NotEqualQuery()
        {
            var list = GetTestList();

            var syntax = _src.Parse("Name != 'Alice Smith'");

            var result = syntax.QueryCollection(list);

            Assert.AreEqual(list.Count - 1, result.records.Length);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
        }


        [TestMethod]
        public void BooleanContains()
        {
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty contains 's'"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty contains True"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty startswith True"); });
            Assert.ThrowsException<FieldTypeMismatch>(() => { _src.Parse("OnDuty endswith True"); });
        }


        [TestMethod]
        public void IsNullQuery()
        {
            var list = GetTestList();

            var syntax = _src.Parse("Name is NULL");
            var result = syntax.QueryCollection(list);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(1, result.records.Length);
            
            // Test the opposite
            syntax = _src.Parse("Name is not NULL");
            result = syntax.QueryCollection(list);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(8, result.records.Length);
        }


        [TestMethod]
        public void ContainsNull()
        {
            var list = GetTestList();
            // Searchlight interprets the word "null" without apostrophes here to be the string value "null"
            // instead of a null.
            var syntax = _src.Parse("Name contains null");

            var result = syntax.QueryCollection(list);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);
            Assert.IsTrue(result.records.Any(p => p.name == "Roderick 'null' Sqlkeywordtest"));
        }


        [TestMethod]
        public void InQuery()
        {
            var list = GetTestList();

            var syntax = _src.Parse("name in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')");

            var result = syntax.QueryCollection(list);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsTrue(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);
        
            // Now run the opposite query
            syntax = _src.Parse("name not in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')"); 
            result = syntax.QueryCollection(list);

            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsFalse(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(7, result.records.Length);
        }


        [TestMethod]
        public void InQueryInt()
        {
            var list = GetTestList();
            // getting not implemented error on this line
            // make sure using right formatting, if so then in operator needs adjustment
            var syntax = _src.Parse("id in (1,2,57)");

            var result = syntax.QueryCollection(list);

            Assert.IsTrue(result.records.Any(p => p.id == 1));
            Assert.IsTrue(result.records.Any(p => p.id == 2));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);
        }

        [TestMethod]
        public void InQueryDecimals()
        {
            var list = GetTestList();

            var syntax = _src.Parse("paycheck in (578.00, 1.234)");

            var result = syntax.QueryCollection(list);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records[0].id == 7);
        }
        
        [TestMethod]
        public void InQueryEmptyList()
        {
            Assert.ThrowsException<EmptyClause>(() => _src.Parse("name in ()"));
            Assert.ThrowsException<EmptyClause>(() => _src.Parse("paycheck > 1 AND name in ()"));
        }
      
        [TestMethod]  
        public void StringEqualsCaseInsensitive()
        {
            var list = GetTestList();

            var syntax = _src.Parse("name eq 'ALICE SMITH'");

            var result = syntax.QueryCollection(list);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);
            
            // Try the inverse
            syntax = _src.Parse("name not eq 'ALICE SMITH'");
            result = syntax.QueryCollection(list);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(list.Count - 1, result.records.Length);
        }
      
        [TestMethod]
        public void DefinedDateOperators()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("hired < TODAY");
            var result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Length == 3 || result.records.Length == 4);

            syntax = _src.Parse("hired < TOMORROW");
            result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);
            
            syntax = _src.Parse("hired < tomorrow");
            result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);
            
            syntax = _src.Parse("hired > YESTERDAY");
            result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Length == 5 || result.records.Length == 6);

            syntax = _src.Parse("hired > NOW");
            result = syntax.QueryCollection(list);
            Assert.AreEqual(4, result.records.Length);
            
            syntax = _src.Parse("hired < NOW");
            result = syntax.QueryCollection(list);
            Assert.AreEqual(5, result.records.Length);
            
            Assert.ThrowsException<FieldTypeMismatch>(() => _src.Parse("hired > yesteryear"));
        }

        [TestMethod]
        public void NormalDateQueries()
        {
            var list = GetTestList();
            
            var syntax = _src.Parse("hired > 2020-01-01");
            var result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == list.Count);
            
            syntax = _src.Parse("hired < 1985-01-01");
            result = syntax.QueryCollection(list);
            Assert.IsFalse(result.records.Any());

            // Now try the opposite
            syntax = _src.Parse("hired not < 1985-01-01");
            result = syntax.QueryCollection(list);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == list.Count);

            syntax = _src.Parse("hired not > 2020-01-01");
            result = syntax.QueryCollection(list);
            Assert.IsFalse(result.records.Any());
        }

        [TestMethod]
        public void SortedQueries()
        {
            // id test ascending and descending
            var list = GetTestList();
            var control = (from item in list orderby item.id ascending select item).ToList();
            var syntax = _src.Parse(null, null, "id ASC");
            var result = syntax.QueryCollection(list);
            
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }
            
            control = (from item in list orderby item.id descending select item).ToList();
            syntax = _src.Parse("", null, "id descending");
            result = syntax.QueryCollection(list);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }
            
            // name test ascending and descending
            control = (from item in list orderby item.name ascending select item).ToList();
            syntax = _src.Parse("", null, "name ASC");
            result = syntax.QueryCollection(list);
            
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }
            
            control = (from item in list orderby item.name descending select item).ToList();
            syntax = _src.Parse("", null, "name DESC");
            result = syntax.QueryCollection(list);
            
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }
            
            // paycheck test ascending and descending
            control = (from item in list orderby item.paycheck ascending select item).ToList();
            syntax = _src.Parse("", null, "paycheck ASC");
            result = syntax.QueryCollection(list);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }
            
            control = (from item in list orderby item.paycheck descending select item).ToList();
            syntax = _src.Parse("", null, "paycheck DESC");
            result = syntax.QueryCollection(list);
            
            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }
            
            // onduty test ascending and descending
            control = (from item in list orderby item.onduty ascending select item).ToList();
            syntax = _src.Parse("", null, "onduty ASC");
            result = syntax.QueryCollection(list);
            
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }
            
            control = (from item in list orderby item.onduty descending select item).ToList();
            syntax = _src.Parse("", null, "onduty DESC");
            result = syntax.QueryCollection(list);
            
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }
            
            // hired test ascending and descending
            control = (from item in list orderby item.hired ascending select item).ToList();
            syntax = _src.Parse("", null, "hired ASC");
            result = syntax.QueryCollection(list);
            
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].hired, control[i].hired);
            }
            
            control = (from item in list orderby item.hired descending select item).ToList();
            syntax = _src.Parse("", null, "hired DESC");
            result = syntax.QueryCollection(list);
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(result.records[i].hired, control[i].hired);
            }
        }

        [TestMethod]
        public void DefaultReturn()
        {
            var list = GetTestList();
            var syntax = _src.Parse("");
            syntax.PageNumber = 0; // default is 0
            syntax.PageSize = 0; // default is 0
            
            var result = syntax.QueryCollection(list);
            
            // return everything
            Assert.AreEqual(list.Count, result.records.Length);
        }
        
        [TestMethod]
        public void PageNumberNoPageSize()
        {
            var list = GetTestList();
            var syntax = _src.Parse("");
            syntax.PageNumber = 2;
            syntax.PageSize = 0; // default is 0

            var result = syntax.QueryCollection(list);
            
            // return everything
            Assert.AreEqual(result.records.Length, list.Count);
        }

        [TestMethod]
        public void PageSizeNoPageNumber()
        {
            var list = GetTestList();
            var syntax = _src.Parse("");
            
            syntax.PageSize = 2;
            syntax.PageNumber = 0; // no page number defaults to 0

            var result = syntax.QueryCollection(list);
            
            // should take the first 2 elements
            Assert.AreEqual(result.records.Length, 2);
        }

        [TestMethod]
        public void PageSizeAndPageNumber()
        {
            var list = GetTestList();
            var syntax = _src.Parse("");
            syntax.PageSize = 1;
            syntax.PageNumber = 2;

            var result = syntax.QueryCollection(list);
            
            Assert.AreEqual(result.records.Length, 1);
        }

        /// <summary>
        /// Searchlight can work using LINQ when the class types match up 
        /// </summary>
        [TestMethod]
        public void QueryPartiallyCompatibleCollection()
        {
            var list = GetCompatibleList();

            // Try a few queries and sorts that _can_ work on a compatible type
            var syntax = _src.Parse("name startswith c");
            var result = syntax.QueryCollection(list);
            Assert.AreEqual("Charlie Compatible", result.records[0].name);
            
            syntax = _src.Parse("name startswith c", null, "onduty asc");
            result = syntax.QueryCollection(list);
            Assert.AreEqual("Charlie Compatible", result.records[0].name);

            // Now try a query and a sort that won't work
            syntax = _src.Parse("name startswith c and id = 57 and hired > 2020-02-01 and onduty = false");
            var ex = Assert.ThrowsException<FieldTypeMismatch>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("id on type CompatibleEmployeeObj", ex.FieldName);

            syntax = _src.Parse("name startswith c", null, "id asc");
            ex = Assert.ThrowsException<FieldTypeMismatch>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("id on type CompatibleEmployeeObj", ex.FieldName);
        }

        [TestMethod]
        public void QueryIncompatibleCollection()
        {
            var list = GetIncompatibleList();
            var syntax = _src.Parse("name startswith a");
            syntax.PageSize = 1;
            syntax.PageNumber = 2;

            var ex = Assert.ThrowsException<FieldNotFound>(() => { _ = syntax.QueryCollection(list); });
            Assert.AreEqual("name", ex.FieldName);
        }
    }
}