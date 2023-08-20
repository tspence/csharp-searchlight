using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            // After release 1.0.0 we can assert that all conjunctions within a compound clause are identical
            var conjunction = $" {Children[0].Conjunction} ";
            var clauses = Children.Select(c => c.ToString()).ToArray();
            return $"({string.Join(conjunction, clauses)})";
        }
    }
}
