using System;
using System.Text;
using Searchlight.Parsing;

namespace Searchlight.Configuration.Default
{
    /// <summary>
    /// Fully qualifies column names by prepending the name of the table.
    /// </summary>
    public class FullyQualifyColumnNames : IColumnify
    {
        private readonly DatabaseType _databaseType;
        private readonly string _tableNamePrefix;

        public FullyQualifyColumnNames(string tableName, DatabaseType databaseType)
        {
            _databaseType = databaseType;

            StringBuilder tablePrefixBuilder = new StringBuilder(tableName);

            // surround the tablenameprefix with the appropriate characters by database type
            switch (databaseType) {
                case DatabaseType.SqlServer:
                    tablePrefixBuilder.Replace(".", "].[");
                    tablePrefixBuilder.Insert(0, "[");
                    tablePrefixBuilder.Append("]");
                    break;

                case DatabaseType.Mysql:
                    tablePrefixBuilder.Replace(".", "`.`");
                    tablePrefixBuilder.Insert(0, "`");
                    tablePrefixBuilder.Append("`");
                    break;
            }

            _tableNamePrefix = tablePrefixBuilder.ToString();
        }

        public string Columnify(string columnName, ClauseType clauseType)
        {
            switch (_databaseType)
            {
                case DatabaseType.SqlServer:
                    return string.Format("{0}.[{1}]", _tableNamePrefix, columnName);
                case DatabaseType.Mysql:
                    return string.Format("{0}.`{1}`", _tableNamePrefix, columnName);
                default:
                    throw new ArgumentOutOfRangeException("Unknown databaset type: " + _databaseType);
            }
        }
    }
}