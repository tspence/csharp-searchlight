﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight.Query;
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace Searchlight.Tests
{
    public class EmployeeTestSuite
    {
        private readonly DataSource _src;
        private readonly List<EmployeeObj> _list;
        private readonly Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> _executor;

        private EmployeeTestSuite(DataSource src, List<EmployeeObj> list,
            Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> executor)
        {
            _src = src;
            _list = list;
            _executor = executor;
        }

        /// <summary>
        /// Validates basic correctness of the query engine against this executor
        /// 
        /// Does not include any case-insensitive / case-sensitive string comparisons
        /// </summary>
        /// <param name="src"></param>
        /// <param name="list"></param>
        /// <param name="executor"></param>
        public static async Task BasicTestSuite(DataSource src, List<EmployeeObj> list,
            Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> executor)
        {
            var suite = new EmployeeTestSuite(src, list, executor);
            await suite.QueryListCollection();
            await suite.NestedClauseQuery();
            await suite.BetweenQuery();
            await suite.StartsWithQuery();
            await suite.EndsWithQuery();
            await suite.ContainsQuery();
            await suite.NotEqualQuery();
            await suite.IsNullQuery();
            await suite.ContainsNull();
            await suite.InQuery();
            await suite.InQueryInt();
            await suite.InQueryDecimals();
            await suite.DefinedDateOperators();
            await suite.NormalDateQueries();
            await suite.SortedQueries();
            await suite.DefaultReturn();
            await suite.PageNumberNoPageSize();
            await suite.PageSizeNoPageNumber();
            await suite.PageSizeAndPageNumber();
        }

        /// <summary>
        /// Validates correctness of this executor's ability to execute case insensitive string comparisons
        /// </summary>
        /// <param name="src"></param>
        /// <param name="list"></param>
        /// <param name="executor"></param>
        public static async Task CaseInsensitiveStringTestSuite(DataSource src, List<EmployeeObj> list,
            Func<SyntaxTree, Task<FetchResult<EmployeeObj>>> executor)
        {
            var suite = new EmployeeTestSuite(src, list, executor);
            await suite.LessThanOrEqualQuery();
            await suite.LessThanQuery();
            await suite.GreaterThanQuery();
            await suite.GreaterThanOrEqualQuery();
            await suite.StringEqualsCaseInsensitive();
        }

        private async Task QueryListCollection()
        {
            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and paycheck le 1000");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause)syntax.Filter[0]).Value.GetValue());
            Assert.AreEqual("paycheck", ((CriteriaClause)syntax.Filter[1]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause)syntax.Filter[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)syntax.Filter[1]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(8, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck <= 1000.0m);
            }
        }

        private async Task NestedClauseQuery()
        {
            // Construct a simple query and check that it comes out correct
            var syntax = _src.Parse("id gt 1 and (paycheck lt 1000 or paycheck gt 1000)");
            Assert.AreEqual(2, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.AND, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual(1, ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Did we get a nested clause?
            var cc = syntax.Filter[1] as CompoundClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual(2, cc.Children.Count);
            Assert.AreEqual("paycheck", ((CriteriaClause)cc.Children[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause)cc.Children[0]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)cc.Children[0]).Value.GetValue());
            Assert.AreEqual("paycheck", ((CriteriaClause)cc.Children[1]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)cc.Children[1]).Operation);
            Assert.AreEqual(1000.0m, ((CriteriaClause)cc.Children[1]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id > 1);
                Assert.IsTrue(e.paycheck is 800.0m or 1200.0m or 10.0m or 578.00m or 123.00m or 987.00m or 632.00m);
            }
        }

        private async Task BetweenQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("id between 2 and 4");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(false, syntax.Filter[0].Negated);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("id", ((BetweenClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause)syntax.Filter[0]).LowerValue.GetValue());
            Assert.AreEqual(4, ((BetweenClause)syntax.Filter[0]).UpperValue.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
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
            Assert.AreEqual("id", ((BetweenClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(2, ((BetweenClause)syntax.Filter[0]).LowerValue.GetValue());
            Assert.AreEqual(4, ((BetweenClause)syntax.Filter[0]).UpperValue.GetValue());
            results = await _executor(syntax);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.id is <= 1 or >= 5);
            }
        }

        private async Task StartsWithQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name startswith 'A'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.StartsWith, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("A", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name[0] == 'A');
            }
        }

        private async Task EndsWithQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name endswith 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.EndsWith, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(2, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name.EndsWith("s", StringComparison.OrdinalIgnoreCase));
            }
        }

        private async Task ContainsQuery()
        {
            // Note that the "between" clause is inclusive
            var syntax = _src.Parse("name contains 's'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.Contains, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("s", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(9, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e != null && e.name.Contains('s', StringComparison.OrdinalIgnoreCase));
            }

            // Now test the opposite
            syntax = _src.Parse("name is not null and name not contains 's'");
            results = await _executor(syntax);
            Assert.AreEqual(0, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(
                    e != null && (e.name == null || !e.name.Contains('s', StringComparison.OrdinalIgnoreCase)));
            }
            
            // Test for the presence of special characters that might cause problems for parsing
            syntax = _src.Parse("name contains '''[Not.Regex(safe{\\^|$'''");
            results = await _executor(syntax);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(e.name.Contains("'[Not.Regex(safe{\\^|$'"));
            }
        }

        private async Task GreaterThanQuery()
        {
            var syntax = _src.Parse("name gt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(8, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) > 0);
            }
        }

        private async Task GreaterThanOrEqualQuery()
        {
            var syntax = _src.Parse("name ge 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.GreaterThanOrEqual, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(7, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers",
                    StringComparison.CurrentCultureIgnoreCase) >= 0);
            }
        }

        private async Task LessThanQuery()
        {
            var syntax = _src.Parse("name lt 'b'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThan, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("b", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(1, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name, "b", StringComparison.CurrentCultureIgnoreCase) < 0);
            }
        }

        private async Task LessThanOrEqualQuery()
        {
            var syntax = _src.Parse("name le 'bob rogers'");
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(ConjunctionType.NONE, syntax.Filter[0].Conjunction);
            Assert.AreEqual("name", ((CriteriaClause)syntax.Filter[0]).Column.FieldName);
            Assert.AreEqual(OperationType.LessThanOrEqual, ((CriteriaClause)syntax.Filter[0]).Operation);
            Assert.AreEqual("bob rogers", ((CriteriaClause)syntax.Filter[0]).Value.GetValue());

            // Execute the query and ensure that each result matches
            var results = await _executor(syntax);
            Assert.AreEqual(3, results.records.Length);
            foreach (var e in results.records)
            {
                Assert.IsTrue(string.Compare(e.name[.."bob rogers".Length], "bob rogers",
                    StringComparison.CurrentCultureIgnoreCase) <= 0);
            }
        }

        private async Task NotEqualQuery()
        {
            var syntax = _src.Parse("Name is null or Name != 'Alice Smith'");
            var result = await _executor(syntax);
            Assert.AreEqual(_list.Count - 1, result.records.Length);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
        }


        private async Task IsNullQuery()
        {
            var syntax = _src.Parse("Name is NULL");
            var result = await _executor(syntax);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(1, result.records.Length);

            // Test the opposite
            syntax = _src.Parse("Name is not NULL");
            result = await _executor(syntax);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.AreEqual(9, result.records.Length);
        }

        private async Task ContainsNull()
        {
            // Searchlight interprets the word "null" without apostrophes here to be the string value "null"
            // instead of a null.
            var syntax = _src.Parse("Name contains null");

            var result = await _executor(syntax);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);
            Assert.IsTrue(result.records.Any(p => p.name == "Roderick 'null' Sqlkeywordtest"));
        }

        private async Task InQuery()
        {
            var syntax = _src.Parse("name in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')");

            var result = await _executor(syntax);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsTrue(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);

            // Now run the opposite query
            syntax = _src.Parse("name is null or name not in ('Alice Smith', 'Bob Rogers', 'Sir Not Appearing in this Film')");
            result = await _executor(syntax);

            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsFalse(result.records.Any(p => p.name == "Bob Rogers"));
            Assert.IsNotNull(result);
            Assert.AreEqual(8, result.records.Length);
        }

        private async Task InQueryInt()
        {
            // getting not implemented error on this line
            // make sure using right formatting, if so then in operator needs adjustment
            var syntax = _src.Parse("id in (1,2,57)");

            var result = await _executor(syntax);

            Assert.IsTrue(result.records.Any(p => p.id == 1));
            Assert.IsTrue(result.records.Any(p => p.id == 2));
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.records.Length);
        }

        private async Task InQueryDecimals()
        {
            var syntax = _src.Parse("paycheck in (578.00, 1.234)");

            var result = await _executor(syntax);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records[0].id == 7);
        }

        private async Task StringEqualsCaseInsensitive()
        {
            var syntax = _src.Parse("name eq 'ALICE SMITH'");

            var result = await _executor(syntax);

            Assert.IsTrue(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.records.Length);

            // Try the inverse
            syntax = _src.Parse("name not eq 'ALICE SMITH'");
            result = await _executor(syntax);
            Assert.IsFalse(result.records.Any(p => p.name == "Alice Smith"));
            Assert.IsNotNull(result);
            Assert.AreEqual(_list.Count - 1, result.records.Length);
        }

        private async Task DefinedDateOperators()
        {
            var syntax = _src.Parse("hired < NOW");
            var result = await _executor(syntax);
            Assert.AreEqual(5, result.records.Length);

            syntax = _src.Parse("hired < NOW + 1");
            result = await _executor(syntax);
            Assert.AreEqual(7, result.records.Length);

            syntax = _src.Parse("hired < NOW + 2");
            result = await _executor(syntax);
            Assert.AreEqual(7, result.records.Length);

            syntax = _src.Parse("hired > NOW - 1");
            result = await _executor(syntax);
            Assert.AreEqual(7, result.records.Length);

            syntax = _src.Parse("hired > NOW");
            result = await _executor(syntax);
            Assert.AreEqual(5, result.records.Length);

            syntax = _src.Parse("hired < NOW");
            result = await _executor(syntax);
            Assert.AreEqual(5, result.records.Length);

            Assert.ThrowsException<FieldTypeMismatch>(() => _src.Parse("hired > yesteryear"));
        }

        private async Task NormalDateQueries()
        {
            var syntax = _src.Parse("hired > 2020-01-01");
            var result = await _executor(syntax);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == _list.Count);

            syntax = _src.Parse("hired < 1985-01-01");
            result = await _executor(syntax);
            Assert.IsFalse(result.records.Any());

            // Now try the opposite
            syntax = _src.Parse("hired not < 1985-01-01");
            result = await _executor(syntax);
            Assert.IsTrue(result.records.Any());
            Assert.IsTrue(result.records.Length == _list.Count);

            syntax = _src.Parse("hired not > 2020-01-01");
            result = await _executor(syntax);
            Assert.IsFalse(result.records.Any());
        }

        private async Task SortedQueries()
        {
            // id test ascending and descending
            var control = (from item in _list orderby item.id ascending select item).ToList();
            var syntax = _src.Parse(null, null, "id ASC");
            var result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }

            control = (from item in _list orderby item.id descending select item).ToList();
            syntax = _src.Parse("", null, "id descending");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].id, control[i].id);
            }

            // name test ascending and descending
            control = (from item in _list orderby item.name ascending select item).ToList();
            syntax = _src.Parse("", null, "name ASC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }

            control = (from item in _list orderby item.name descending select item).ToList();
            syntax = _src.Parse("", null, "name DESC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].name, control[i].name);
            }

            // paycheck test ascending and descending
            control = (from item in _list orderby item.paycheck ascending select item).ToList();
            syntax = _src.Parse("", null, "paycheck ASC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }

            control = (from item in _list orderby item.paycheck descending select item).ToList();
            syntax = _src.Parse("", null, "paycheck DESC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].paycheck, control[i].paycheck);
            }

            // onduty test ascending and descending
            control = (from item in _list orderby item.onduty ascending select item).ToList();
            syntax = _src.Parse("", null, "onduty ASC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }

            control = (from item in _list orderby item.onduty descending select item).ToList();
            syntax = _src.Parse("", null, "onduty DESC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                Assert.AreEqual(result.records[i].onduty, control[i].onduty);
            }

            //
            // Sorting by DateTime, ascending and descending.
            //
            // Some database systems may store dates with slightly different precision than C# does internally.
            // This means that comparing strict equality is not appropriate.  We instead assert that there
            // is less than 16ms of drift between the two dates, which is less than one frame on a speedrun
            // of classic Super Mario Bros.
            //

            control = (from item in _list orderby item.hired ascending select item).ToList();
            syntax = _src.Parse("", null, "hired ASC");
            result = await _executor(syntax);

            for (var i = 0; i < _list.Count; i++)
            {
                var ts = result.records[i].hired - control[i].hired;
                Assert.IsTrue(ts.TotalMilliseconds < 16.0);
            }

            control = (from item in _list orderby item.hired descending select item).ToList();
            syntax = _src.Parse("", null, "hired DESC");
            result = await _executor(syntax);
            for (var i = 0; i < _list.Count; i++)
            {
                var ts = result.records[i].hired - control[i].hired;
                Assert.IsTrue(ts.TotalMilliseconds < 16.0);
            }
        }

        private async Task DefaultReturn()
        {
            var syntax = _src.Parse("");
            syntax.PageNumber = 0; // default is 0
            syntax.PageSize = 0; // default is 0

            var result = await _executor(syntax);

            // return everything
            Assert.AreEqual(_list.Count, result.records.Length);
        }

        private async Task PageNumberNoPageSize()
        {
            var syntax = _src.Parse("");
            syntax.PageNumber = 2;
            syntax.PageSize = 0; // default is 0

            var result = await _executor(syntax);

            // return everything
            Assert.AreEqual(result.records.Length, _list.Count);
        }

        private async Task PageSizeNoPageNumber()
        {
            var syntax = _src.Parse("");

            syntax.PageSize = 2;
            syntax.PageNumber = 0; // no page number defaults to 0

            var result = await _executor(syntax);

            // should take the first 2 elements
            Assert.AreEqual(result.records.Length, 2);
        }

        private async Task PageSizeAndPageNumber()
        {
            var syntax = _src.Parse("");
            syntax.PageSize = 1;
            syntax.PageNumber = 2;

            var result = await _executor(syntax);

            Assert.AreEqual(result.records.Length, 1);
        }
    }
}