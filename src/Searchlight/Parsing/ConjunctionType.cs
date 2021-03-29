
namespace Searchlight.Parsing
{
    /// <summary>
    /// Defines the type of conjunction between two clauses
    /// </summary>
    public enum ConjunctionType
    {
        /// <summary>
        /// Represents the end of the filter
        /// </summary>
        NONE,

        /// <summary>
        /// These indicate that the filter will continue to have more statements
        /// </summary>
        AND,
        OR
    }
}
