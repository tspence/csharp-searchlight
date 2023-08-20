using System;
using System.Collections.Generic;
#pragma warning disable CS1591

namespace Searchlight
{
    /// <summary>
    /// This class contains all whitelisted SQL tokens that can be placed into a legitimate SQL string
    /// </summary>
    internal static class StringConstants
    {
        /// <summary>
        /// Represents the list of query expressions we would recognize if the user passed them in a filter.
        /// The "KEY" represents the value we allow the user to provide.
        /// The "VALUE" represents the actual string we will place in the SQL.
        /// </summary>
        internal static readonly Dictionary<string, OperationType> RECOGNIZED_QUERY_EXPRESSIONS = new Dictionary<string, OperationType>
        {
            // Basic SQL query expressions
            { "=",  OperationType.Equals  },
            { ">",  OperationType.GreaterThan  },
            { ">=", OperationType.GreaterThanOrEqual },
            { "<>", OperationType.NotEqual },
            { "!=", OperationType.NotEqual },
            { "<",  OperationType.LessThan },
            { "<=", OperationType.LessThanOrEqual },

            // Microsoft's published REST standard alternatives for query expressions
            { "EQ", OperationType.Equals },
            { "GT", OperationType.GreaterThan  },
            { "GE", OperationType.GreaterThanOrEqual },
            { "GTE", OperationType.GreaterThanOrEqual },
            { "NE", OperationType.NotEqual },
            { "LT", OperationType.LessThan },
            { "LE", OperationType.LessThanOrEqual },
            { "LTE", OperationType.LessThanOrEqual },

            // Slightly less common query expressions
            { "BETWEEN",        OperationType.Between     },
            { "IN",             OperationType.In          },
            { "STARTSWITH",     OperationType.StartsWith  },
            { "CONTAINS",       OperationType.Contains    },
            { "ENDSWITH",       OperationType.EndsWith    },
            { "IS",             OperationType.IsNull      },
        };

        /// <summary>
        /// Represents the list of query expressions we would recognize if the user passed them in a filter
        /// </summary>

        /// <summary>
        /// Represents the list of tokens that can close an "IN" clause
        /// </summary>
        public static readonly string[] SAFE_LIST_TOKENS = new string[] { ",", ")" };

        /// <summary>
        /// Represents the list of conjunctions that can occur between tests, and the insertion values that we should apply
        /// </summary>
        internal static readonly Dictionary<string, string> SAFE_CONJUNCTIONS = new Dictionary<string, string>
        {
            { "(", "(" },
            { ")", ")" },
            { "AND", " AND " },
            { "OR", " OR " },
            { "NOT", " NOT " },
        };

        /// <summary>
        /// Represents the list of single-character operators for tokenization
        /// </summary>
        public static readonly char[] SINGLE_CHARACTER_OPERATORS = new char[] { '=', '>', '<', '(', ')', ',', '!' };

        /// <summary>
        /// Represents a single quote character for tokenization of strings
        /// </summary>
        public static readonly char SINGLE_QUOTE = '\'';

        /// <summary>
        /// Represents an open parenthesis character for tokenization of strings
        /// </summary>
        public static readonly string OPEN_PARENTHESIS = "(";

        /// <summary>
        /// Represents a close parenthesis character for tokenization of strings
        /// </summary>
        public static readonly string CLOSE_PARENTHESIS = ")";

        /// <summary>
        /// Represents descending sort
        /// </summary>
        public static readonly string DESCENDING = "DESCENDING";
        public static readonly string DESCENDING_ABBR = "DESC";

        /// <summary>
        /// Represents ascending sort
        /// </summary>
        public static readonly string ASCENDING = "ASCENDING";
        public static readonly string ASCENDING_ABBR = "ASC";

        /// <summary>
        /// User specified an inverse operation
        /// </summary>
        public static readonly string NOT = "NOT";

        /// <summary>
        /// Used for the "BETWEEN" clause parsing and boolean AND clauses
        /// </summary>
        public static readonly string AND = "AND";

        /// <summary>
        /// Used for boolean OR clauses
        /// </summary>
        public static readonly string OR = "OR";

        /// <summary>
        /// Used for parsing the "IS [NOT] NULL" command
        /// </summary>
        public static readonly string NULL = "NULL";

        /// <summary>
        /// Used for identifying the separator in a list
        /// </summary>
        public static readonly string COMMA = ",";

        /// <summary>
        /// Used for date math
        /// </summary>
        public static readonly string ADD = "+";

        /// <summary>
        /// Used for date math
        /// </summary>
        public static readonly string SUBTRACT = "-";

        /// <summary>
        /// Used as shorthand for typing today's date
        /// </summary>
        internal static readonly Dictionary<string, Func<DateTime>> DEFINED_DATES = new Dictionary<string, Func<DateTime>>
        {
            {"NOW", () => DateTime.UtcNow},
            {"TODAY", () => DateTime.Today},
            {"TOMORROW", () => DateTime.Today.AddDays(1)},
            {"YESTERDAY", () => DateTime.Today.AddDays(-1)}
        };
    }
}