using Searchlight.Configuration;
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
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// The field name of the default sort field, if none are specified.
        /// This is necessary to ensure reliable pagination.
        /// </summary>
        public string DefaultSortField { get; set; }

        /// <summary>
        /// This function produces a list of optional commands that can be specified in the $include parameter
        /// </summary>
        public Func<IEnumerable<OptionalCommand>> Commands { get; set; }
    }
}
