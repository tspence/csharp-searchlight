using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
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
