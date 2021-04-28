using System;
using System.Collections.Generic;
using System.Linq;

namespace Searchlight.Nesting
{
    public class OptionalCollectionCommand : ICommand
    {
        public OptionalCollectionCommand(string name, SearchlightCollection coll)
        {
            _aliases = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(name))
            {
                _aliases.Add(name.Trim().ToUpperInvariant());
            }

            if (coll.Aliases != null)
            {
                foreach (var alias in coll.Aliases)
                {
                    _aliases.Add(alias.Trim().ToUpperInvariant());
                }
            }
            _collection = coll;
        }
        
        private readonly HashSet<string> _aliases;
        private readonly SearchlightCollection _collection;
        
        public bool MatchesName(string commandName)
        {
            return _aliases.Contains(commandName?.Trim().ToUpperInvariant());
        }

        public void Apply(SqlQuery sql)
        {
            var num = sql.ResultSetClauses.Count() + 1;
            sql.ResultSetClauses.Add($"SELECT * FROM {_collection.ForeignTableName} t{num} INNER JOIN #temp ON t{num}.{_collection.ForeignTableKey} = #temp.{_collection.LocalKey};");
        }

        public void Preview(FetchRequest request)
        {
            // Nothing
        }
    }
}