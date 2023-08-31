using Searchlight.Nesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Searchlight.Query
{
    /// <summary>
    /// Internal representation of a query, after it has been verified by SafeQueryParser
    /// </summary>
    public class SyntaxTree
    {
        /// <summary>
        /// The data source for this request
        /// </summary>
        public DataSource Source { get; set; }

        /// <summary>
        /// The original filter that was provided to the query, for reference
        /// </summary>
        public string OriginalFilter { get; set; }

        /// <summary>
        /// Represents commands that were specified by the "$include" parameter
        /// </summary>
        public List<ICommand> Includes { get; set; }

        /// <summary>
        /// Returns true if a particular flag has been enabled in this syntax request
        /// </summary>
        /// <param name="flagName">The name of the flag to check</param>
        /// <returns>True if this flag is enabled</returns>
        public bool HasFlag(string flagName)
        {
            return (from f in Flags where String.Equals(f.Name, flagName, StringComparison.CurrentCultureIgnoreCase) select f).Any();
        }

        /// <summary>
        /// Lists flags that were specified by choices in the "$include" parameter
        /// </summary>
        public List<SearchlightFlag> Flags { get; set; }

        /// <summary>
        /// The prioritized list of sort statements specified by the "$orderBy" parameter
        /// </summary>
        public List<SortInfo> OrderBy { get; set; }

        /// <summary>
        /// The list of clauses in the "$filter" statement
        /// </summary>
        public List<BaseClause> Filter { get; set; }

        /// <summary>
        /// For pagination, which (zero-based) page are we viewing?
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// For pagination, how large is each page?
        /// </summary>
        public int? PageSize { get; set; }
    }
}
