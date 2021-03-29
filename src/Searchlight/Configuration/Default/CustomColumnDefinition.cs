using Searchlight.Parsing;
using System;
using System.Collections.Generic;

namespace Searchlight.Configuration.Default
{
    public class CustomColumnDefinition : ISafeColumnDefinition
    {
        private readonly Dictionary<string, ColumnInfo> _fieldDict;

        public CustomColumnDefinition()
        {
            _fieldDict = new Dictionary<string, ColumnInfo>();
        }

        #region Builder pattern
        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public CustomColumnDefinition WithColumn(string columnName, Type columnType, Type enumType)
        {
            _fieldDict[columnName.ToUpper()] = new ColumnInfo(columnName, columnName, columnType, enumType);
            return this;
        }

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public CustomColumnDefinition WithRenamingColumn(string filterName, string columnName, Type columnType, Type enumType)
        {
            // Allow the API caller to either specify the model name
            _fieldDict[filterName.ToUpper()] = new ColumnInfo(filterName, columnName, columnType, enumType);

            // Allowing them to use the Entity name preserves compatibility with filters written in the past
            _fieldDict[columnName.ToUpper()] = new ColumnInfo(filterName, columnName, columnType, enumType);
            return this;
        }
        #endregion

        #region Interface implementation
        /// <summary>
        /// Identify all columns
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ColumnInfo> GetColumnDefinitions()
        {
            return _fieldDict.Values;
        }

        /// <summary>
        /// Identify a single column by its token
        /// </summary>
        /// <param name="filterToken"></param>
        /// <returns></returns>
        public ColumnInfo IdentifyColumn(string filterToken)
        {
            ColumnInfo ci = null;
            _fieldDict.TryGetValue(filterToken?.ToUpper(), out ci);
            return ci;
        }
        #endregion
    }
}
