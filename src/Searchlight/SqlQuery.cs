using System;
using System.Collections.Generic;
using Searchlight.Query;

namespace Searchlight {
    /// <summary>
    /// Information about an SQL Query that can be executed against a database
    /// </summary>
    public class SqlQuery
    {
        /// <summary>
        /// The syntax tree used to construct this query
        /// </summary>
        public SyntaxTree Syntax { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        
        public SqlQuery()
        {
            Parameters = new Dictionary<string, object>();
            ParameterTypes = new Dictionary<string, Type>();
            ResultSetClauses = new List<string>();
        }

        /// <summary>
        /// The text of this SQL command
        /// </summary>
        public string CommandText { get; set; }

        /// <summary>
        /// The list of parameterized SQL values to apply to this query text
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// The filter criteria (WHERE clause) for this SQL statement 
        /// </summary>
        public string WhereClause { get; set; }
        
        /// <summary>
        /// The ordering criteria (ORDERBY clause) for this SQL statement
        /// </summary>
        public string OrderByClause { get; set;  }
        
        /// <summary>
        /// The list of extra information fetched in a multi-result-set query
        /// </summary>
        public List<string> ResultSetClauses { get; }

        /// <summary>
        /// The list of parameter types to use
        /// </summary>
        public Dictionary<string, Type> ParameterTypes { get; set; }

        internal string AddParameter(object p, Type t)
        {
            var num = Parameters.Count + 1;
            var name = $"@p{num}";
            Parameters.Add(name, p);
            ParameterTypes.Add(name, t);
            return name;
        }
    }
}