using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents a compound clause, one surrounded in (parenthesis)
    /// </summary>
    public class CompoundClause : BaseClause
    {
        /// <summary>
        /// The children of this compound clause
        /// </summary>
        public List<BaseClause> Children { get; set; }
    }
}
