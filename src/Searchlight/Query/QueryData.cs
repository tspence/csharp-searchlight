using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    /// <summary>
    /// Internal representation of a query, after it has been verified by SafeQueryParser
    /// </summary>
    public class QueryData
    {
        /// <summary>
        /// Represents commands specified by the "$include" parameter
        /// </summary>
        public List<string> Includes { get; set; }

        /// <summary>
        /// The prioritized list of sort statements specified by the "$orderBy" parameter
        /// </summary>
        public List<SortInfo> OrderBy { get; set; }

        /// <summary>
        /// The list of clauses in the "$filter" statement
        /// </summary>
        public List<BaseClause> Filter { get; set; }
    }
}
