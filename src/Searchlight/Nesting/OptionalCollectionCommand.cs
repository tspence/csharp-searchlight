using System;
using System.Collections.Generic;
using System.Linq;
using Searchlight.Exceptions;
using Searchlight.Parsing;

namespace Searchlight.Nesting
{
    public class OptionalCollectionCommand : ICommand
    {
        private readonly HashSet<string> _aliases;
        private readonly SearchlightCollection _collection;
        private readonly DataSource _parentTable;
        private readonly string _fieldName; 
        
        public OptionalCollectionCommand(DataSource table, SearchlightCollection coll, string fieldName)
        {
            _aliases = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                _aliases.Add(fieldName.Trim().ToUpperInvariant());
            }

            if (coll.Aliases != null)
            {
                foreach (var alias in coll.Aliases)
                {
                    _aliases.Add(alias.Trim().ToUpperInvariant());
                }
            }
            _collection = coll;
            _parentTable = table;
            _fieldName = fieldName;
        }
        
        public bool MatchesName(string commandName)
        {
            return _aliases.Contains(commandName?.Trim().ToUpperInvariant());
        }

        public void Apply(SqlQuery sql)
        {
            if (_parentTable == null) throw new InvalidCollection() { TableName = "Unknown", CollectionName = _fieldName, CollectionErrorMessage = "Table not found" };
            var parentKey = _parentTable.IdentifyColumn(_collection.LocalKey);
            if (parentKey == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = $"Local key column {_collection.LocalKey} not found" };
            var foreignTable = _parentTable.Engine?.FindTable(_collection.ForeignTableName);
            if (foreignTable == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = "Foreign table (collection table) not found" };
            var foreignKey = foreignTable.IdentifyColumn(_collection.ForeignTableKey);
            if (foreignKey == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = $"Foreign key {_collection.ForeignTableKey} not found on table {foreignTable.TableName}" };

            var num = sql.ResultSetClauses.Count() + 1;
            sql.ResultSetClauses.Add($"SELECT * FROM {foreignTable.TableName} t{num} INNER JOIN #temp ON t{num}.{foreignKey.OriginalName} = #temp.{parentKey.OriginalName};");
        }

        public void Preview(FetchRequest request)
        {
            // Nothing
        }
    }
}