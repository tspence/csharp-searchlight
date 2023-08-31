using System;
using System.Reflection;
using Searchlight.Exceptions;

namespace Searchlight.Nesting
{
    /// <summary>
    /// A nested command that handles a collection
    /// </summary>
    public class CollectionCommand : ICommand
    {
        private readonly string[] _aliases;
        private readonly SearchlightCollection _collection;
        private readonly DataSource _parentTable;
        private readonly string _fieldName;
        private readonly PropertyInfo _property;

        /// <summary>
        /// Name of the command
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return _property.Name;
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="table"></param>
        /// <param name="coll"></param>
        /// <param name="property"></param>
        public CollectionCommand(DataSource table, SearchlightCollection coll, PropertyInfo property)
        {
            _property = property;
            _fieldName = property.Name;
            _aliases = coll.Aliases ?? Array.Empty<string>();
            _collection = coll;
            _parentTable = table;
        }
        
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="sql"></param>
        /// <exception cref="InvalidCollection"></exception>
        public void Apply(SqlQuery sql)
        {
            if (_parentTable == null) throw new InvalidCollection() { TableName = "Unknown", CollectionName = _fieldName, CollectionErrorMessage = "Table not found" };
            var parentKey = _parentTable.IdentifyColumn(_collection.KeyName);
            if (parentKey == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = $"Local key column {_collection.KeyName} not found" };
            var foreignTable = _parentTable.Engine?.FindTable(_collection.ForeignTableName ?? _property?.PropertyType.GetElementType()?.Name);
            if (foreignTable == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = "Foreign table (collection table) not found" };
            var fkName = _collection.ForeignTableKey ?? _collection.KeyName;
            var foreignKey = foreignTable.IdentifyColumn(fkName);
            if (foreignKey == null) throw new InvalidCollection() { TableName = _parentTable.TableName, CollectionName = _fieldName, CollectionErrorMessage = $"Foreign key {fkName} not found on table {foreignTable.TableName}" };

            var num = sql.ResultSetClauses.Count + 1;
            sql.ResultSetClauses.Add($"SELECT t{num}.* FROM {foreignTable.TableName} t{num} INNER JOIN #temp ON t{num}.{foreignKey.OriginalName} = #temp.{parentKey.OriginalName};");
        }

        /// <summary>
        /// Fetch all aliases for this command
        /// </summary>
        /// <returns></returns>
        public string[] GetAliases()
        {
            return _aliases;
        }
    }
}