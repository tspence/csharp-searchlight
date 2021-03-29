
namespace Searchlight.Parsing
{
    /// <summary>
    /// Define the data source type being queried
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
}