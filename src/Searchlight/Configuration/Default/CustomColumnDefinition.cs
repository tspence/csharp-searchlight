using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public CustomColumnDefinition WithColumn(string columnName, Type columnType, Type enumType)
        {
            return WithRenamingColumn(columnName, columnName, null, columnType, enumType);
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
            AddName(filterName, columnInfo);
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    AddName(alias, columnInfo);
                }
            }
            return this;
        }

        private void AddName(string name, ColumnInfo col)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var upperName = name.ToUpper();
            if (_fieldDict.ContainsKey(upperName))
            {
                var existing = _fieldDict[upperName];
                throw new DuplicateName()
                    {ExistingColumn = existing.OriginalName, ConflictingColumn = col.OriginalName, ConflictingName = upperName};
            }

            _fieldDict[upperName] = col;
        }

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
    }
}
