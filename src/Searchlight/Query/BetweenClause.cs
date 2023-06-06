using Searchlight.Parsing;
using Searchlight.Expressions;

namespace Searchlight.Query
{
    /// <summary>
    /// A query criteria of the form "X between low and high"
    /// </summary>
    public class BetweenClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// Lower value in the between test
        /// </summary>
        public IExpressionValue LowerValue { get; set; }

        /// <summary>
        /// Upper value in the between test
        /// </summary>
        public IExpressionValue UpperValue { get; set; }
        
        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            return $"{Column.FieldName} between {LowerValue} and {UpperValue}";
        }
    }
}
