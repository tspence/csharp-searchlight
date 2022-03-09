using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    /// <summary>
    /// The root node of the Searchlight abstract syntax tree
    /// </summary>
    public class BaseClause
    {
        /// <summary>
        /// This value is true if the result of this test is to be inverted
        /// </summary>
        public bool Negated { get; set; }

        /// <summary>
        /// If this clause has another one after it, this conjunction is AND or OR.
        /// If this is the last clause, the conjunction is NONE.
        /// </summary>
        public ConjunctionType Conjunction { get; set; }
    }
}
