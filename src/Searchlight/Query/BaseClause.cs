using Searchlight.Parsing;

namespace Searchlight.Query
{
    /// <summary>
    /// The root node of the Searchlight abstract syntax tree
    /// </summary>
    public class BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public ColumnInfo Column { get; set; }
        
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
