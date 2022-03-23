using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;
using Searchlight.Query;
using System.Linq;
using System.Threading.Tasks;
using Searchlight.Exceptions;
using Searchlight.Expressions;

namespace Searchlight.Tests
{
    [SearchlightModel(DefaultSort = nameof(Name))]
    public class TestStrictMode
    {
        [SearchlightField] public string Name { get; set; }
        [SearchlightField] public string Description { get; set; }
        public string NotASearchlightField { get; set; }
    }

    [SearchlightModel(DefaultSort = nameof(Name))]
    public class TestFieldRenaming
    {
        [SearchlightField(OriginalName = "field_name")]
        public string Name { get; set; }

        [SearchlightField(Aliases = new string[] {"desription", "DescriptionText"})]
        public string Description { get; set; }

        [SearchlightField] public string NotASearchlightField { get; set; }
    }

    [TestClass]
    public class ParseModelTests
    {
        [TestMethod]
        public void TestLimitedFields()
        {
            var source = DataSource.Create(null, typeof(TestStrictMode), AttributeMode.Strict);
            var columns = source.GetColumnDefinitions().ToArray();
            Assert.AreEqual(2, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);

            // Attempt to query a field that does not exist
            string originalFilter = "a = 'test'";
            var ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("NotASearchlightField", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);
        }

        [TestMethod]
        public void TestExpansiveFields()
        {
            var source = DataSource.Create(null, typeof(TestStrictMode), AttributeMode.Loose);
            var columns = source.GetColumnDefinitions().ToArray();
            Assert.AreEqual(3, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);
            Assert.AreEqual("NotASearchlightField", columns[2].FieldName);
            Assert.AreEqual(typeof(string), columns[2].FieldType);

            // Attempt to query a field that does not exist
            string originalFilter = "a = 'test'";
            var ex = Assert.ThrowsException<FieldNotFound>(() => source.ParseFilter(originalFilter));
            Assert.AreEqual("a", ex.FieldName);
            Assert.AreEqual(originalFilter, ex.OriginalFilter);

            // Attempt to query a field that does exist, but is not permitted to be queried
            originalFilter = "NotASearchlightField = 'Hello'";
            var clauses = source.ParseFilter(originalFilter);
            Assert.AreEqual(1, clauses.Count);
            var cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("NotASearchlightField", cc.Column.FieldName);
        }

        [TestMethod]
        public void TestFieldRenaming()
        {
            var source = DataSource.Create(null, typeof(TestFieldRenaming), AttributeMode.Strict);
            var columns = source.GetColumnDefinitions().ToArray();
            Assert.AreEqual(3, columns.Length);
            Assert.AreEqual("Name", columns[0].FieldName);
            Assert.AreEqual(typeof(string), columns[0].FieldType);

            // This field includes backwards compatibility for alternative names
            Assert.AreEqual("Description", columns[1].FieldName);
            Assert.AreEqual(typeof(string), columns[1].FieldType);
            Assert.AreEqual(2, columns[1].Aliases.Length);
            Assert.AreEqual("desription",
                columns[1].Aliases[
                    0]); // Example: "I misspelled the field name in version 1 of the API, so I had to rename it"
            Assert.AreEqual("DescriptionText",
                columns[1].Aliases[
                    1]); // Example: "This field was originally known as DescriptionText, but our new standards made us change it"

            // Attempt to query a field using its old name
            var clauses = source.ParseFilter("desription contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count);
            var cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);

            // Attempt to query a field using its old name
            clauses = source.ParseFilter("DescriptionText contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count);
            cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);

            // Attempt to query a field using its old name
            clauses = source.ParseFilter("Description contains 'Blockchain'");
            Assert.AreEqual(1, clauses.Count);
            cc = clauses[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            Assert.AreEqual("Description", cc.Column.FieldName);
        }

        [SearchlightModel]
        public class TestFieldConflicts
        {
            [SearchlightField(Aliases = new string[] {"description"})]
            public string Name { get; set; }

            [SearchlightField(Aliases = new string[] {"desription", "DescriptionText"})]
            public string Description { get; set; }
        }

        [TestMethod]
        public void TestNamingConflicts()
        {
            var ex = Assert.ThrowsException<DuplicateName>(() =>
            {
                var source = DataSource.Create(null, typeof(TestFieldConflicts), AttributeMode.Strict);
            });
            Assert.AreEqual("DESCRIPTION", ex.ConflictingName);
            Assert.AreEqual("Name", ex.ExistingColumn);
            Assert.AreEqual("Description", ex.ConflictingColumn);
        }

        [TestMethod]
        public void TestNonSearchlightModel()
        {
            // "THIS" isn't a searchlight model; in strict mode it doesn't work
            Assert.ThrowsException<NonSearchlightModel>(() =>
            {
                var source = DataSource.Create(null, this.GetType(), AttributeMode.Strict);
            });

            // But if I try it in loose mode, will skip NonSearchlightModel error
            // And throw a InvalidDefaultSort error
            Assert.ThrowsException<InvalidDefaultSort>(() =>
            {
                DataSource.Create(null, this.GetType(), AttributeMode.Loose);
            });
        }

        [SearchlightModel(DefaultSort = "name")]
        public class TestWithDefaultSort
        {
            [SearchlightField(Aliases = new string[] {"fullName"})]
            public string Name { get; set; }

            [SearchlightField(Aliases = new string[] {"DescriptionText"})]
            public string Description { get; set; }
        }

        [SearchlightModel]
        public class TestWithNoDefaultSort
        {
            [SearchlightField(Aliases = new string[] {"fullName"})]
            public string Name { get; set; }
            
            [SearchlightField(Aliases = new string[] {"DescriptionText"})]
            public string Description { get; set; }
        }

        [SearchlightModel(DefaultSort = "Invalid")]
        public class TestInvalidDefaultSort
        {
            [SearchlightField(Aliases = new string[] {"fullName"})]
            public string Name { get; set; }
            
            [SearchlightField(Aliases = new string[] {"DescriptionText"})]
            public string Description { get; set; }
        }

        [TestMethod]
        public void TestDefaultSort()
        {
            var source = DataSource.Create(null, typeof(TestWithDefaultSort), AttributeMode.Strict);
            var query = source.Parse(null, null, null);
            Assert.AreEqual(1, query.OrderBy.Count);
            Assert.AreEqual("Name", query.OrderBy[0].Column.FieldName);
            Assert.AreEqual(SortDirection.Ascending, query.OrderBy[0].Direction);
        }

        [TestMethod]
        public void TestNoDefaultSortException()
        {
            Assert.ThrowsException<InvalidDefaultSort>(() =>
            {
                DataSource.Create(null, typeof(TestWithNoDefaultSort), AttributeMode.Strict);
            });
        }

        [TestMethod]
        public void TestWithInvalidDefaultSort()
        {
            Assert.ThrowsException<InvalidDefaultSort>(() =>
            {
                DataSource.Create(null, typeof(TestInvalidDefaultSort), AttributeMode.Strict);
            });
        }

        [TestMethod]
        public void TestEngineAssemblyParsing()
        {
            var engine = new SearchlightEngine()
                .AddAssembly(this.GetType().Assembly);
            Assert.IsNotNull(engine.FindTable("TestWithDefaultSort"));
            Assert.IsNotNull(engine.FindTable("BookReservation"));
            Assert.IsNotNull(engine.FindTable("BookCopy"));
            
            // This is the list of expected errors
            Assert.AreEqual(5, engine.ModelErrors.Count);
            Assert.IsTrue(engine.ModelErrors.Any(err =>
            {
                if (err is InvalidDefaultSort defSort)
                {
                    return (defSort.Table == "EmployeeObj");
                }
                return false;
            }));
            Assert.IsTrue(engine.ModelErrors.Any(err =>
            {
                if (err is DuplicateName duplicateName)
                {
                    return (duplicateName.Table == "TestFieldConflicts");
                }
                return false;
            }));
            Assert.IsTrue(engine.ModelErrors.Any(err =>
            {
                if (err is InvalidDefaultSort defSort)
                {
                    return (defSort.Table == "TestWithNoDefaultSort");
                }
                return false;
            }));
            Assert.IsTrue(engine.ModelErrors.Any(err =>
            {
                if (err is InvalidDefaultSort defSort)
                {
                    return (defSort.Table == "TestInvalidDefaultSort");
                }
                return false;
            }));
        }

        [TestMethod]
        public void TestFetchRequestNullFilter()
        {
            // Arrange
            var source = DataSource.Create(null, typeof(TestWithDefaultSort), AttributeMode.Strict);
            var fetchRequest = new FetchRequest();
            fetchRequest.Append(null);

            // Act
            var query = source.Parse(fetchRequest);

            // Assert
            Assert.AreEqual(0, query.Filter.Count);
        }

        [TestMethod]
        public void TestFetchRequestSingleFilter()
        {
            // Arrange
            var source = DataSource.Create(null, typeof(TestWithDefaultSort), AttributeMode.Strict);
            var fetchRequest = new FetchRequest();
            fetchRequest.Append("Name eq Test");

            // Act
            var query = source.Parse(fetchRequest);

            // Assert
            Assert.AreEqual(1, query.Filter.Count);

            var firstClause = query.Filter.First() as CriteriaClause;
            Assert.IsNotNull(firstClause);

            Assert.AreEqual(ConjunctionType.NONE, firstClause.Conjunction);
            Assert.AreEqual("Name", firstClause.Column.FieldName);
            Assert.AreEqual(OperationType.Equals, firstClause.Operation);
            Assert.AreEqual("Test", firstClause.Value.GetValue());
        }

        [TestMethod]
        public void TestFetchRequestMultipleFilters()
        {
            // Arrange
            var source = DataSource.Create(null, typeof(TestWithDefaultSort), AttributeMode.Strict);
            var fetchRequest = new FetchRequest();
            fetchRequest.Append("Name eq Test");
            fetchRequest.Append("Description != Whatever");

            // Act
            var query = source.Parse(fetchRequest);

            // Assert
            Assert.AreEqual(2, query.Filter.Count);

            var firstClause = query.Filter.First() as CompoundClause;
            Assert.IsNotNull(firstClause);
            var firstCriteria = firstClause.Children.Single() as CriteriaClause;
            Assert.IsNotNull(firstCriteria);

            Assert.AreEqual(ConjunctionType.AND, firstClause.Conjunction);
            Assert.AreEqual(1, firstClause.Children.Count);
            Assert.AreEqual("Name", firstCriteria.Column.FieldName);
            Assert.AreEqual(OperationType.Equals, firstCriteria.Operation);
            Assert.AreEqual("Test", firstCriteria.Value.GetValue());

            var secondClause = query.Filter[1] as CriteriaClause;
            Assert.IsNotNull(secondClause);
            Assert.AreEqual("Description", secondClause.Column.FieldName);
            Assert.AreEqual(OperationType.NotEqual, secondClause.Operation);
            Assert.AreEqual("Whatever", secondClause.Value.GetValue());
        }
        
        [SearchlightModel(DefaultSort = "Name DESC")]
        public class TestDefaultSortDirectionClass
        {
            [SearchlightField(OriginalName = "field_name")]
            public string Name { get; set; }
        }
        
        [SearchlightModel(DefaultSort = "Name ascending")]
        public class TestOtherDefaultSortDirectionClass
        {
            [SearchlightField(OriginalName = "field_name")]
            public string Name { get; set; }
        }

        [TestMethod]
        public void TestDefaultSortDirection()
        {
            // Arrange
            var source = DataSource.Create(null, typeof(TestDefaultSortDirectionClass), AttributeMode.Strict);
            var fetchRequest = new FetchRequest();
            fetchRequest.Append("Name eq Test");

            // Act
            var syntax = source.Parse(fetchRequest);

            // Assert
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(1, syntax.OrderBy.Count);
            Assert.AreEqual("Name", syntax.OrderBy[0].Column.FieldName);
            Assert.AreEqual(SortDirection.Descending, syntax.OrderBy[0].Direction);
        }

        [TestMethod]
        public void TestOtherDefaultSortDirection()
        {
            // Arrange
            var source = DataSource.Create(null, typeof(TestOtherDefaultSortDirectionClass), AttributeMode.Strict);
            var fetchRequest = new FetchRequest();
            fetchRequest.Append("Name eq Test");

            // Act
            var syntax = source.Parse(fetchRequest);

            // Assert
            Assert.AreEqual(1, syntax.Filter.Count);
            Assert.AreEqual(1, syntax.OrderBy.Count);
            Assert.AreEqual("Name", syntax.OrderBy[0].Column.FieldName);
            Assert.AreEqual(SortDirection.Ascending, syntax.OrderBy[0].Direction);
        }

        [SearchlightModel(DefaultSort = "name")]
        public class TestWithDateField
        {
            [SearchlightField]
            public string Name { get; set; }

            [SearchlightField]
            public DateTime Hired { get; set; }
        }

        [TestMethod]
        public async Task QueryComputedCriteria()
        {
            var source = DataSource.Create(null, typeof(TestWithDateField), AttributeMode.Strict);
            var syntax = source.Parse("hired > TODAY - 30");
            Assert.AreEqual(1, syntax.Filter.Count);
            var cc = syntax.Filter[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            var ic = cc.Value as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("TODAY", ic.Root);
            Assert.AreEqual(-30, ic.Offset);

            source = DataSource.Create(null, typeof(TestWithDateField), AttributeMode.Strict);
            syntax = source.Parse("hired > NOW + 1");
            Assert.AreEqual(1, syntax.Filter.Count);
            cc = syntax.Filter[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            ic = cc.Value as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("NOW", ic.Root);
            Assert.AreEqual(1, ic.Offset);
            
            // Verify that the computed value actually moves in time
            var firstValue = (DateTime)ic.GetValue();
            var daysDiff = firstValue - DateTime.UtcNow;
            Assert.AreEqual(1, Math.Round(daysDiff.TotalDays)); // In testing this was often 0.999 etc
            await Task.Delay(1);
            var secondValue = (DateTime)ic.GetValue();
            var timeSpan = secondValue - firstValue;
            Assert.IsTrue(timeSpan.TotalMilliseconds >= 1);

            source = DataSource.Create(null, typeof(TestWithDateField), AttributeMode.Strict);
            syntax = source.Parse("hired > TOMORROW + 0");
            Assert.AreEqual(1, syntax.Filter.Count);
            cc = syntax.Filter[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            ic = cc.Value as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("TOMORROW", ic.Root);
            Assert.AreEqual(0, ic.Offset);
            
            source = DataSource.Create(null, typeof(TestWithDateField), AttributeMode.Strict);
            syntax = source.Parse("hired > YESTERDAY - 0");
            Assert.AreEqual(1, syntax.Filter.Count);
            cc = syntax.Filter[0] as CriteriaClause;
            Assert.IsNotNull(cc);
            ic = cc.Value as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("YESTERDAY", ic.Root);
            Assert.AreEqual(0, ic.Offset);
            
            source = DataSource.Create(null, typeof(TestWithDateField), AttributeMode.Strict);
            syntax = source.Parse("hired BETWEEN YESTERDAY - 14 AND YESTERDAY - 7");
            Assert.AreEqual(1, syntax.Filter.Count);
            var bc = syntax.Filter[0] as BetweenClause;
            Assert.IsNotNull(bc);
            ic = bc.LowerValue as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("YESTERDAY", ic.Root);
            Assert.AreEqual(-14, ic.Offset);
            ic = bc.UpperValue as ComputedDateValue;
            Assert.IsNotNull(ic);
            Assert.AreEqual("YESTERDAY", ic.Root);
            Assert.AreEqual(-7, ic.Offset);
        }
    }
}