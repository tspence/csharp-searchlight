using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents a simple one-operator clause
    /// </summary>
    public class CriteriaClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// Operation for testing
        /// </summary>
        public OperationType Operation { get; set; }

        /// <summary>
        /// Value to test against
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            return $"{Column.FieldName} {Operation} {Value}";
        }
    }
}
