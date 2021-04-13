
namespace Searchlight.Parsing
{
    /// <summary>
    /// Defines the operation type for a clause
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Failed to parse
        /// </summary>
        Unknown,

        /// <summary>
        /// Simple operations
        /// </summary>
        Equals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        NotEqual,

        /// <summary>
        /// Complex Operations
        /// </summary>
        Between,
        In,
        Contains,
        StartsWith,
        EndsWith,
        IsNull,
    }
}
