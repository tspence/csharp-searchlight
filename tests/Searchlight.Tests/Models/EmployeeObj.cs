using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable InconsistentNaming

namespace Searchlight.Tests.Models
{
    [SearchlightModel(DefaultSort = nameof(name))]
    public class EmployeeObj
    {
        public enum EmployeeType
        {
            FullTime,
            PartTime,
            Contract
        }
        
        [SearchlightField(FieldType = typeof(string))]
        public string name { get; set; }
        
        [SearchlightField(FieldType = typeof(int))]
        public int id { get; set; }
        
        [SearchlightField(FieldType = typeof(DateTime))]
        public DateTime hired { get; set; }
        
        [BsonRepresentation(BsonType.Decimal128)]
        [SearchlightField(FieldType = typeof(decimal))]
        public decimal paycheck { get; set; }
        
        [SearchlightField(FieldType = typeof(bool))]
        public bool onduty { get; set; }
        
        [SearchlightField(FieldType = typeof(int), EnumType = typeof(EmployeeType))]
        public EmployeeType type { get; set; }

        public static List<EmployeeObj> GetTestList()
        {
            return new List<EmployeeObj>
            {
                new()
                    { hired = DateTime.UtcNow.AddMinutes(-1), id = 1, name = "Alice Smith", onduty = true, paycheck = 1000.00m },
                new()
                {
                    hired = DateTime.UtcNow.AddMonths(-1),
                    id = 2,
                    name = "Bob Rogers",
                    onduty = true,
                    paycheck = 1000.00m,
                    type = EmployeeType.FullTime
                },
                new()
                {
                    hired = DateTime.UtcNow.AddMonths(-6),
                    id = 3,
                    name = "Charlie Prentiss",
                    onduty = false,
                    paycheck = 800.0m,
                    type = EmployeeType.PartTime
                },
                new()
                {
                    hired = DateTime.UtcNow.AddMonths(-12),
                    id = 4,
                    name = "Danielle O'Shea",
                    onduty = false,
                    paycheck = 1200.0m,
                    type = EmployeeType.Contract
                },
                new()
                {
                    hired = DateTime.UtcNow.AddMonths(1),
                    id = 5,
                    name = "Ernest Nofzinger",
                    onduty = true,
                    paycheck = 1000.00m
                },
                new()
                    { hired = DateTime.UtcNow.AddMonths(4), id = 6, name = null, onduty = false, paycheck = 10.00m },
                new()
                {
                    hired = DateTime.UtcNow.AddMonths(2),
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
                    id = 9,
                    name = "Carol 'Starting Soon!' Yamashita",
                    onduty = false,
                    paycheck = 987.00m,
                },
                new()
                {
                    hired = DateTime.UtcNow.AddHours(15),
                    id = 10,
                    name = "Barnabas '[Not.Regex(safe{\\^|$' Ellsworth",
                    onduty = true,
                    paycheck = 632.00m,
                }
            };
        }
    }

    public class CompatibleEmployeeObj
    {
        public string name { get; set; }
        public int? id { get; set; }
        public string hired { get; set; }
        public string paycheck { get; set; }
        public bool onduty { get; set; }

        public static List<CompatibleEmployeeObj> GetCompatibleList()
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
    }

    public class IncompatibleEmployeeObj
    {
        public string FullName { get; set; }
        public int Identifier { get; set; }
        public decimal? IncompatibleMongoField { get; set; }

        public static List<IncompatibleEmployeeObj> GetIncompatibleList()
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
    }
}