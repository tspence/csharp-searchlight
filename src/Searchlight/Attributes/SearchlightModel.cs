using System;
using System.ComponentModel.DataAnnotations;

namespace Searchlight
{
    /// <summary>
    /// Represents a field that is permitted to be used as a filter or sort-by column in the SafeParser
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SearchlightModel : Attribute
    {
        /// <summary>
        /// The underlying name for this model in the data store
        /// </summary>
        public string OriginalName { get; set; }

        /// <summary>
        /// If Searchlight should recognize this table by any other aliases, list them here
        /// </summary>
        public string[] Aliases { get; set; }
        
        /// <summary>
        /// The maximum number of parameters that can be used when querying this model
        /// </summary>
        public int? MaximumParameters { get; set; }
        
        /// <summary>
        /// The default sort criteria to use when none are specified
        /// </summary>
        public string DefaultSort { get; set; }
    }
}
