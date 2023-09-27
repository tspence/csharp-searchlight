using Searchlight.Parsing;
using Searchlight.Expressions;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents a simple one-operator clause
    /// </summary>
    public class CriteriaClause : BaseClause
    {
        /// <summary>
        /// Operation for testing
        /// </summary>
        public OperationType Operation { get; set; }

        /// <summary>
        /// Value to test against
        /// </summary>
        public IExpressionValue Value { get; set; }

        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            return $"{Column.FieldName} {Operation} {Value}";
        }
    }
}
