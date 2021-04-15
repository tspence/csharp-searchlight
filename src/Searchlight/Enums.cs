namespace Searchlight
{
    /// <summary>
    /// Differing types of clauses for columnification
    /// </summary>
    public enum ClauseType
    {
        Select,
        Where,
        GroupBy,
        OrderBy
    }

    ///
    public class X {

    }

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
        Ascending,
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
    /// Some SQL data sources require slightly different syntax
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// Use LINQ expression trees to query a generic collection in memory
        /// </summary>
        GenericCollection,

        /// <summary>
        /// Use T/SQL to query a microsoft SQL server database
        /// </summary>
        SqlServer,

        /// <summary>
        /// Use MySql queries
        /// </summary>
        Mysql
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
}