using Searchlight.Parsing;
using System;
using System.Collections.Generic;

namespace Searchlight.Configuration.Default
{
    public class CustomColumnDefinition : ISafeColumnDefinition
    {
        private readonly Dictionary<string, ColumnInfo> _fieldDict;
        private readonly List<ColumnInfo> _columns;

        public CustomColumnDefinition()
        {
            _fieldDict = new Dictionary<string, ColumnInfo>();
            _columns = new List<ColumnInfo>();
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
            var columnInfo = new ColumnInfo(columnName, columnName, null, columnType, enumType);
            _fieldDict[columnName.ToUpper()] = columnInfo;
            _columns.Add(columnInfo);
            return this;
        }

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public CustomColumnDefinition WithRenamingColumn(string filterName, string columnName, string[] aliases,  Type columnType, Type enumType)
        {
            var columnInfo = new ColumnInfo(filterName, columnName, aliases, columnType, enumType);
            _columns.Add(columnInfo);

            // Allow the API caller to either specify either the model name or one of the aliases
            _fieldDict[filterName.ToUpper()] = columnInfo;
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    _fieldDict[alias.ToUpper()] = columnInfo;
                }
            }
            return this;
        }
        #endregion

        #region Interface implementation
        public IEnumerable<ColumnInfo> GetColumnDefinitions()
        {
            return _columns;
        }

        public IEnumerable<string> ColumnNames()
        {
            return _fieldDict.Keys;
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
