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

        [TestMethod]
        public void ParseTokensAndPositions()
        {
            // Parse another filter, but this time compare both text and positions
            var filters = "CompanyReturnId=123 and ((FilingYear>    2013) or (name startswith 'a')) and Errors is null";
            var list = Tokenizer.GenerateTokens(filters).TokenQueue.ToList();
            Assert.AreEqual(21, list.Count);
            Assert.AreEqual("CompanyReturnId", list[0].Value);
            Assert.AreEqual(0, list[0].StartPosition);
            Assert.AreEqual("=", list[1].Value);
            Assert.AreEqual(15, list[1].StartPosition);
            Assert.AreEqual("123", list[2].Value);
            Assert.AreEqual(16, list[2].StartPosition);
            Assert.AreEqual("and", list[3].Value);
            Assert.AreEqual(20, list[3].StartPosition);
            Assert.AreEqual("(", list[4].Value);
            Assert.AreEqual(24, list[4].StartPosition);
            Assert.AreEqual("(", list[5].Value);
            Assert.AreEqual(25, list[5].StartPosition);
            Assert.AreEqual("FilingYear", list[6].Value);
            Assert.AreEqual(26, list[6].StartPosition);
            Assert.AreEqual(">", list[7].Value);
            Assert.AreEqual(36, list[7].StartPosition);
            Assert.AreEqual("2013", list[8].Value);
            Assert.AreEqual(41, list[8].StartPosition);
            Assert.AreEqual(")", list[9].Value);
            Assert.AreEqual(45, list[9].StartPosition);
            Assert.AreEqual("or", list[10].Value);
            Assert.AreEqual(47, list[10].StartPosition);
            Assert.AreEqual("(", list[11].Value);
            Assert.AreEqual(50, list[11].StartPosition);
            Assert.AreEqual("name", list[12].Value);
            Assert.AreEqual(51, list[12].StartPosition);
            Assert.AreEqual("startswith", list[13].Value);
            Assert.AreEqual(56, list[13].StartPosition);
            Assert.AreEqual("a", list[14].Value);
            Assert.AreEqual(67, list[14].StartPosition);
            Assert.AreEqual(")", list[15].Value);
            Assert.AreEqual(70, list[15].StartPosition);
            Assert.AreEqual(")", list[16].Value);
            Assert.AreEqual(71, list[16].StartPosition);
            Assert.AreEqual("and", list[17].Value);
            Assert.AreEqual(73, list[17].StartPosition);
            Assert.AreEqual("Errors", list[18].Value);
            Assert.AreEqual(77, list[18].StartPosition);
            Assert.AreEqual("is", list[19].Value);
            Assert.AreEqual(84, list[19].StartPosition);
            Assert.AreEqual("null", list[20].Value);
            Assert.AreEqual(87, list[20].StartPosition);
        }
    }
}
