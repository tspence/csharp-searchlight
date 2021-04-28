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
        /// The name of the foreign table
        /// </summary>
        public string ForeignTableName { get; set; }
        
        /// <summary>
        /// The local key to use to join this table to the foreign table
        /// </summary>
        public string LocalKey { get; set; }
        
        /// <summary>
        /// The key to use on the foreign table to join with the local key
        /// </summary>
        public string ForeignTableKey { get; set; }
    }
}