using Microsoft.VisualStudio.TestTools.UnitTesting;
using Searchlight;
using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Searchlight.Tests
{
    [TestClass]
    public class TokenizerTests
    {
        [DataTestMethod]
        [DataRow("WHERE field1 = 'value AND Id = 123")]
        public void NonterminatedString(string filter)
        {
            var tokens = Tokenizer.GenerateTokens(filter);
            Assert.IsTrue(tokens.HasUnterminatedLiteral);
        }

        [TestMethod]
        public void CheckStandardFilters()
        {
            // Parse a date time pattern
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string filters = String.Format("EffectiveDate > '{0}' OR MaintenanceDate > '{0}' OR TotalSalesEffDate > '{0}' OR SalesEffDate > '{0}' OR UseEffDate > '{0}'", date);
            var list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(19, list.Count);
            Assert.AreEqual("EffectiveDate", list[0].Value);
            Assert.AreEqual(">", list[1].Value);
            Assert.AreEqual(date, list[2].Value);
            Assert.AreEqual("OR", list[3].Value);
            Assert.AreEqual("MaintenanceDate", list[4].Value);
            Assert.AreEqual(">", list[5].Value);
            Assert.AreEqual(date, list[6].Value);
            Assert.AreEqual("OR", list[7].Value);
            Assert.AreEqual("TotalSalesEffDate", list[8].Value);
            Assert.AreEqual(">", list[9].Value);
            Assert.AreEqual(date, list[10].Value);
            Assert.AreEqual("OR", list[11].Value);
            Assert.AreEqual("SalesEffDate", list[12].Value);
            Assert.AreEqual(">", list[13].Value);
            Assert.AreEqual(date, list[14].Value);
            Assert.AreEqual("OR", list[15].Value);
            Assert.AreEqual("UseEffDate", list[16].Value);
            Assert.AreEqual(">", list[17].Value);
            Assert.AreEqual(date, list[18].Value);

            // Parse a string with embedded single quotes
            filters = "TestValue = 'Ted''s Home' AND Possessive = 'Cat''s cradle'";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(7, list.Count);
            Assert.AreEqual("TestValue", list[0].Value);
            Assert.AreEqual("=", list[1].Value);
            Assert.AreEqual("Ted's Home", list[2].Value);
            Assert.AreEqual("AND", list[3].Value);
            Assert.AreEqual("Possessive", list[4].Value);
            Assert.AreEqual("=", list[5].Value);
            Assert.AreEqual("Cat's cradle", list[6].Value);

            // Parse a filter with parenthesis
            filters = "(TestValue = 'Ted''s Home') AND (Possessive = 'Cat''s cradle' OR test = 1)";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(15, list.Count);
            Assert.AreEqual("(", list[0].Value);
            Assert.AreEqual("TestValue", list[1].Value);
            Assert.AreEqual("=", list[2].Value);
            Assert.AreEqual("Ted's Home", list[3].Value);
            Assert.AreEqual(")", list[4].Value);
            Assert.AreEqual("AND", list[5].Value);
            Assert.AreEqual("(", list[6].Value);
            Assert.AreEqual("Possessive", list[7].Value);
            Assert.AreEqual("=", list[8].Value);
            Assert.AreEqual("Cat's cradle", list[9].Value);
            Assert.AreEqual("OR", list[10].Value);
            Assert.AreEqual("test", list[11].Value);
            Assert.AreEqual("=", list[12].Value);
            Assert.AreEqual("1", list[13].Value);
            Assert.AreEqual(")", list[14].Value);

            // Parse another filter
            filters = "CompanyReturnId=123 and ((FilingYear>2013) or (FilingYear=2013 and FilingMonth>1))";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual("CompanyReturnId", list[0].Value);
            Assert.AreEqual("=", list[1].Value);
            Assert.AreEqual("123", list[2].Value);
            Assert.AreEqual("and", list[3].Value);
            Assert.AreEqual("(", list[4].Value);
            Assert.AreEqual("(", list[5].Value);
            Assert.AreEqual("FilingYear", list[6].Value);
            Assert.AreEqual(">", list[7].Value);
            Assert.AreEqual("2013", list[8].Value);
            Assert.AreEqual(")", list[9].Value);
            Assert.AreEqual("or", list[10].Value);
            Assert.AreEqual("(", list[11].Value);
            Assert.AreEqual("FilingYear", list[12].Value);
            Assert.AreEqual("=", list[13].Value);
            Assert.AreEqual("2013", list[14].Value);
            Assert.AreEqual("and", list[15].Value);
            Assert.AreEqual("FilingMonth", list[16].Value);
            Assert.AreEqual(">", list[17].Value);
            Assert.AreEqual("1", list[18].Value);
            Assert.AreEqual(")", list[19].Value);
            Assert.AreEqual(")", list[20].Value);

            // Parse a filter with != or <>
            // Usage: Workflow ScheduleFetch
            filters = "FilingTypeId != 2";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("FilingTypeId", list[0].Value);
            Assert.AreEqual("!=", list[1].Value);
            Assert.AreEqual("2", list[2].Value);

            filters = "FilingTypeId <> 2";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("FilingTypeId", list[0].Value);
            Assert.AreEqual("<>", list[1].Value);
            Assert.AreEqual("2", list[2].Value);

            // Test as per Dustin Welden
            filters = "UserName = 'Bob\\'";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("UserName", list[0].Value);
            Assert.AreEqual("=", list[1].Value);
            Assert.AreEqual("Bob\\", list[2].Value);

            // Test as per Dustin Welden
            filters = "UserName = 'Bob'''";
            list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("UserName", list[0].Value);
            Assert.AreEqual("=", list[1].Value);
            Assert.AreEqual("Bob'", list[2].Value);
        }
    }
}
