using Searchlight.Parsing;

namespace Searchlight.Query
{
    /// <summary>
    /// A query criteria in the form "X is null" or "X is not null"
    /// </summary>
    public class IsNullClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public ColumnInfo Column { get; set; }
        
        /// <summary>
        /// Render this criteria in a readable string
        /// </summary>
        public override string ToString()
        {
            return $"{Column.FieldName} is {(this.Negated ? "not" : "")} null";
        }
    }
}
