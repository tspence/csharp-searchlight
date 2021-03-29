using Searchlight.Parsing;
using System.Collections.Generic;

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