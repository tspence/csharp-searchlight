#pragma warning disable CS1591
namespace Searchlight
{
    /// <summary>
    /// Determines the strictness of Searchlight parsing
    /// </summary>
    public enum AttributeMode {
        /// <summary>
        /// Not recommended - Allows developers to query any field on the model, even fields not tagged with the `SearchlightField` annotation.
        /// This is potentially dangerous because a developer could accidentally add a field they do not intend to expose to external partners,
        /// but it allows for more rapid development.
        /// </summary>
        Loose = 0,

        /// <summary>
        /// Recommended - Only allows developers to query against fields tagged with the `SearchlightField` annotation.
        /// </summary>
        Strict = 1,
    }

    /// <summary>
    /// Indicate the direction of a particular sort statement
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Indicates a sort where lower value items are before higher value items
        /// </summary>
        Ascending,
        /// <summary>
        /// Indicates a sort where higher value items are before lower value items
        /// </summary>
        Descending,
    }

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

    public enum SqlDialect
    {
        MicrosoftSqlServer,
        PostgreSql,
    }
}