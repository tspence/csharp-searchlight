using System.Collections.Generic;
using Searchlight.Parsing;

namespace Searchlight.Configuration
{
    public interface ISafeColumnDefinition
    {
        /// <summary>
        /// Returns the collection of valid columns
        /// </summary>
        /// <returns></returns>
        IEnumerable<ColumnInfo> GetColumnDefinitions();

        /// <summary>
        /// Identify one column by a token
        /// </summary>
        ColumnInfo IdentifyColumn(string filterToken);
    }
}