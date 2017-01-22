using System;
using System.Collections.Generic;
using System.Linq;
using Searchlight.Configuration;
using Searchlight.Configuration.Default;
using Searchlight.Parsing;
using Searchlight.Query;
using Searchlight.Exceptions;
using NUnit.Framework;
using Searchlight.DataSource;

namespace Searchlight.Tests.Parsing
{
    public class SafeQueryParserTests
    {
        private SearchlightDataSource _source;

        public SafeQueryParserTests()
        {
            _source = new SearchlightDataSource();
            _source.ColumnDefinitions = new CustomColumnDefinition()
                .WithColumn("a", typeof(String), null)
                .WithColumn("b", typeof(Int32), null)
                .WithColumn("colLong", typeof(Int64), null)
                .WithColumn("colNullableGuid", typeof(Nullable<Guid>), null)
                .WithColumn("colULong", typeof(UInt64), null)
                .WithColumn("colNullableULong", typeof(Nullable<UInt64>), null)
                .WithColumn("colGuid", typeof(Guid), null);
            _source.Columnifier = new NoColumnify();
            _source.DatabaseType = DatabaseType.Mysql;
        }

        [Test(Description = "Parser.IncorrectFieldValueType")]
        public void IncorrectFieldValueType()
        {
            string originalFilter = "a = 'test' and b = 'Hello!'";
            var ex = Assert.Throws<FieldValueException>(() => SafeQueryParser.ParseFilter(originalFilter, _source);
            Assert.AreEqual("b", ex.FieldName);
            Assert.AreEqual("System.Int32", ex.FieldType);
            Assert.AreEqual("Hello!", ex.FieldValue);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [Test(Description = "Parser.AllParenthesis")]
        public void AllParenthesis()
        {
            // Basic problem: if you never close a parenthesis that's a syntax error
            var ex1 = Assert.Throws<TrailingConjunctionException>(() => SafeQueryParser.ParseFilter("(((((((((((", _source));

            // If you unbalance your parenthesis, that's a syntax error
            var ex2 = Assert.Throws<OpenClauseException>(() => SafeQueryParser.ParseFilter("((((((((((()))", _source));
            var ex3 = Assert.Throws<OpenClauseException>(() => SafeQueryParser.ParseFilter("((())))))))))))", _source));

            // If you forget to supply any actual criteria, that's a syntax error
            var ex4 = Assert.Throws<EmptyClauseException>(() => SafeQueryParser.ParseFilter("()", _source));
        }

        [Test(Description = "Parser.OrderByParseTest")]
        public void OrderByParseTest()
        {
            var result = SafeQueryParser.ParseOrderBy("a, b DESC", _source);
            Assert.AreEqual("a", result[0].Column
            Assert.AreEqual("a ASC, b DESC", result.Expression);

            Assert.Throws<FieldNameException>(() => SafeQueryParser.ParseOrderBy("c, d ASC", _source));
        }

        [Test(Description = "Parser.FilterParseTest")]
        public void FilterParseTest()
        {
            WhereClause actual = _parser.ParseWhereClause("a = 'booya' AND b != 1");
            Assert.AreEqual("a = @p1 AND b <> @p2", actual.ValidatedFilter);
            Assert.AreEqual(2, actual.SqlParameters.ParameterNames.Count());

            // TODO: way more tests
        }

        [Test(Description = "Parser.FullyQualifySelectWhereAndOrderBy.MySQL")]
        public void FullyQualifySelectWhereAndOrderBy()
        {
            var safeColumns = new CustomColumnDefinition()
                .WithColumn("a", typeof(String), null)
                .WithColumn("b", typeof(Int32), null);

            var p2 = new SafeQueryParser(safeColumns,
                new FullyQualifyColumnNames("MyTable", DatabaseType.Mysql),
                DatabaseType.Mysql);

            Assert.AreEqual("`MyTable`.`a`, `MyTable`.`b`", p2.ParseSelectClause("a, b").Expression);
            Assert.AreEqual("`MyTable`.`a` = @p1 AND `MyTable`.`b` <> @p2", p2.ParseWhereClause("a = 'booya' AND b != 2").ValidatedFilter);
            Assert.AreEqual("`MyTable`.`a` ASC, `MyTable`.`b` DESC", p2.ParseOrderByClause("a ASC, b DESC", "a").Expression);
        }

        [Test(Description = "Parser.FullyQualifySelectWhereAndOrderBy.SQLServer")]
        public void FullyQualifySelectWhereAndOrderBySqlServer()
        {
            var safeColumns = new CustomColumnDefinition()
                .WithColumn("a", typeof(String), null)
                .WithColumn("b", typeof(Int32), null);

            var p2 = new SafeQueryParser(safeColumns,
                new FullyQualifyColumnNames("MyTable", DatabaseType.SqlServer),
                DatabaseType.SqlServer);

            Assert.AreEqual("[MyTable].[a], [MyTable].[b]", p2.ParseSelectClause("a, b").Expression);
            Assert.AreEqual("[MyTable].[a] = @p1 AND [MyTable].[b] <> @p2", p2.ParseWhereClause("a = 'booya' AND b != 2").ValidatedFilter);
            Assert.AreEqual("[MyTable].[a] ASC, [MyTable].[b] DESC", p2.ParseOrderByClause("a ASC, b DESC", "a").Expression);
        }

        [Test(Description = "Parser.NullInWhereClause")]
        public void NullInWhereClause()
        {
            Assert.AreEqual("a IS NULL", _parser.ParseWhereClause("a is null").ValidatedFilter);
            Assert.AreEqual("a IS NOT NULL", _parser.ParseWhereClause("a is not null").ValidatedFilter);
            Assert.AreEqual("(a IS NOT NULL) OR (a IS NULL)", _parser.ParseWhereClause("(a is not null) or (a is null)").ValidatedFilter);
        }

        [Test(Description = "Parser.OnlyConjunctions")]
        public void OnlyConjunctions()
        {
            string s;

            // Silly example
            Assert.Throws<TrailingConjunctionException>(() => s = _parser.ParseWhereClause("AND ( ) OR ").ValidatedFilter);

            // Realistic example of a forgetful customer
            Assert.Throws<TrailingConjunctionException>(() => s = _parser.ParseWhereClause("a = b OR ").ValidatedFilter);

            // Realistic example of a quirky but valid customer request
            s = _parser.ParseWhereClause("(a = 'test' OR b = 1)").ValidatedFilter;
            Assert.AreEqual("(a = @p1 OR b = @p2)", s);
        }

        [Test(Description = "Parser.AllQueryExpressions")]
        public void AllQueryExpressions()
        {
            string s;

            // Try all basic query expression types - should succeed
            Assert.AreEqual("a = @p1", _parser.ParseWhereClause("a = 'test'").ValidatedFilter);
            Assert.AreEqual("a = @p1", _parser.ParseWhereClause("a eq 'test'").ValidatedFilter);
            Assert.AreEqual("a > @p1", _parser.ParseWhereClause("a > 'test'").ValidatedFilter);
            Assert.AreEqual("a > @p1", _parser.ParseWhereClause("a gt 'test'").ValidatedFilter);
            Assert.AreEqual("a >= @p1", _parser.ParseWhereClause("a >= 'test'").ValidatedFilter);
            Assert.AreEqual("a >= @p1", _parser.ParseWhereClause("a ge 'test'").ValidatedFilter);
            Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a <> 'test'").ValidatedFilter);
            Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a != 'test'").ValidatedFilter);
            Assert.AreEqual("a <> @p1", _parser.ParseWhereClause("a ne 'test'").ValidatedFilter);
            Assert.AreEqual("a < @p1", _parser.ParseWhereClause("a < 'test'").ValidatedFilter);
            Assert.AreEqual("a < @p1", _parser.ParseWhereClause("a lt 'test'").ValidatedFilter);
            Assert.AreEqual("a <= @p1", _parser.ParseWhereClause("a <= 'test'").ValidatedFilter);
            Assert.AreEqual("a <= @p1", _parser.ParseWhereClause("a le 'test'").ValidatedFilter);

            // Try slightly more complex query expression types - should succeed
            Assert.AreEqual("a BETWEEN @p1 AND @p2", _parser.ParseWhereClause("a between 'test1' and 'test9'").ValidatedFilter);
            Assert.AreEqual("a IN (@p1, @p2)", _parser.ParseWhereClause("a in ('test', 'test2')").ValidatedFilter);
            Assert.AreEqual("a LIKE @p1", _parser.ParseWhereClause("a like 'test%'").ValidatedFilter);
            Assert.AreEqual("a IS NULL", _parser.ParseWhereClause("a is null").ValidatedFilter);
            Assert.AreEqual("a IS NOT NULL", _parser.ParseWhereClause("a is not null").ValidatedFilter);

            // Now try some that fail
            Assert.Throws<ParserSyntaxException>(() => s = _parser.ParseWhereClause("a REALLYSHOULDBE 'test'").ValidatedFilter);
            Assert.Throws<ParserSyntaxException>(() => s = _parser.ParseWhereClause("a !<= 'test'").ValidatedFilter);
        }

        [Test(Description = "Parser.TypeComparisons")]
        public void TypeComparisons()
        {
            // Test the Int64
            var w = _parser.ParseWhereClause("collong eq 123456789123456");
            Assert.AreEqual("colLong = @p1", w.ValidatedFilter);
            Assert.AreEqual(123456789123456, w.SqlParameters.Get<long>("p1"));

            // Test the guid
            w = _parser.ParseWhereClause(String.Format("colguid eq '{0}'", Guid.Empty.ToString()));
            Assert.AreEqual("colGuid = @p1", w.ValidatedFilter);
            Assert.AreEqual(Guid.Empty, w.SqlParameters.Get<Guid>("p1"));

            // Test the nullable guid
            w = _parser.ParseWhereClause(String.Format("colNullableGuid is null or colNullableGuid = '{0}'", Guid.Empty.ToString()));
            Assert.AreEqual("colNullableGuid IS NULL OR colNullableGuid = @p1", w.ValidatedFilter);
            Assert.AreEqual(Guid.Empty, w.SqlParameters.Get<Guid>("p1"));

            // Test the ULONG and nullable ULONG
            w = _parser.ParseWhereClause("colULong > 12345 or colNullableULong = 6789456");
            Assert.AreEqual("colULong > @p1 OR colNullableULong = @p2", w.ValidatedFilter);
            Assert.AreEqual(12345UL, w.SqlParameters.Get<UInt64>("p1"));
            Assert.AreEqual(6789456UL, w.SqlParameters.Get<Nullable<UInt64>>("p2"));

            // Test the ULONG and nullable ULONG when compared to a boolean - necessary for redshift
            w = _parser.ParseWhereClause("colULong = true OR colULong = false");
            Assert.AreEqual("colULong = @p1 OR colULong = @p2", w.ValidatedFilter);
            Assert.AreEqual(1UL, w.SqlParameters.Get<UInt64>("p1"));
            Assert.AreEqual(0UL, w.SqlParameters.Get<UInt64>("p2"));

            // Nullable variant
            w = _parser.ParseWhereClause("colNullableULong = true OR colNullableULong = false");
            Assert.AreEqual("colNullableULong = @p1 OR colNullableULong = @p2", w.ValidatedFilter);
            Assert.AreEqual(1UL, w.SqlParameters.Get<UInt64>("p1"));
            Assert.AreEqual(0UL, w.SqlParameters.Get<UInt64>("p2"));
        }
    }
}
