using System.Collections.Generic;
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
            // TODO: This piece of code raises a worry.  What happens if you mix AND and OR within a compound
            // clause?  e.g. (A and B or C) - what order will they be parsed in?  A and (b or c)? (a and b) or c?
            // We should make this deterministic and require that all clauses within a parenthesis have the same
            // conjunction.
            var sb = new StringBuilder();
            sb.Append("(");
            var numChildren = Children?.Count ?? 0;
            for (var i = 0; i < numChildren; i++)
            {
                sb.Append(Children[i]);
                if (i < numChildren)
                {
                    sb.Append(" ");
                    sb.Append(Children[i].Conjunction);
                    sb.Append(" ");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
