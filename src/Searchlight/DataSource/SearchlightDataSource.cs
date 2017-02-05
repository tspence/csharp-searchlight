using Searchlight.Configuration;
using Searchlight.Configuration.Default;
using Searchlight.Nesting;
using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.DataSource
{
    /// <summary>
    /// Represents a data source used to validate queries
    /// </summary>
    public class SearchlightDataSource
    {
        /// <summary>
        /// Definitions of columns
        /// </summary>
        public ISafeColumnDefinition ColumnDefinitions { get; set; }

        /// <summary>
        /// Formatting for the output
        /// </summary>
        public IColumnify Columnifier { get; set; }
        public DataSourceType DatabaseType { get; set; }

        /// <summary>
        /// The field name of the default sort field, if none are specified.
        /// This is necessary to ensure reliable pagination.
        /// </summary>
        public string DefaultSortField { get; set; }

        /// <summary>
        /// This function produces a list of optional commands that can be specified in the $include parameter
        /// </summary>
        public Func<IEnumerable<OptionalCommand>> Commands { get; set; }


        #region Setup
        /// <summary>
        /// Create a searchlight data source based on an in-memory collection
        /// </summary>
        /// <typeparam name="T">The underlying data type being queried</typeparam>
        /// <param name="source">The collection to be used as the source</param>
        /// <param name="queryAllProperties">If true, all properties on the underlying data type will be queryable</param>
        /// <returns></returns>
        public static SearchlightDataSource FromCollection<T>(IEnumerable<T> source, bool queryAllProperties = true)
        {
            SearchlightDataSource src = new SearchlightDataSource();
            src.ColumnDefinitions = new EntityColumnDefinitions(typeof(T));
            src.Columnifier = new NoColumnify();
            src.DatabaseType = DataSourceType.GenericCollection;
            return src;
        }
        #endregion
    }
}
