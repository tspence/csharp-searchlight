using System;

namespace Searchlight
{
    /// <summary>
    /// Apply this attribute to a field that represents an optional collection that can be fetched
    /// </summary>
    public class SearchlightCollection : Attribute
    {
        /// <summary>
        /// A list of aliases for this collection, if any
        /// </summary>
        public string[] Aliases { get; set; }
        
        /// <summary>
        /// The name of the foreign table. If null, uses the underlying collection type name.
        /// </summary>
        public string ForeignTableName { get; set; }
        
        /// <summary>
        /// The name of the key to use to join this table to the foreign table
        /// </summary>
        public string KeyName { get; set; }
        
        /// <summary>
        /// The name of the column to join on the foreign table. If null, uses `KeyName`.
        /// </summary>
        public string ForeignTableKey { get; set; }
    }
}