using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Searchlight.Expressions;

namespace Searchlight.Query
{
    /// <summary>
    /// A query criteria in the form of "X in (A, B, C)"
    /// </summary>
    public class InClause : BaseClause
    {
        /// <summary>
        /// The field to test
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// The list of values to test against
        /// </summary>
        public List<IExpressionValue> Values { get; set; }
        
        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            return $"{Column.FieldName} in ({string.Join(", ", Values)})";
        }
    }
}
