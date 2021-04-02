using System;

namespace Searchlight
{
    /// <summary>
    /// Represents a field that is permitted to be used as a filter or sort-by column in the SafeParser
    /// </summary>
    public class SearchlightModel : Attribute
    {
        /// <summary>
        /// Information about the name of the field and its type in the database, if that type is different from the type the user sees.
        /// You can specify the type the user sees as well as the type as it is stored in the database.
        /// </summary>
        /// <param name="originalName">If the model is known by a different name in the underlying data store, specify it here</param>
        /// <param name="fieldType"></param>
        public SearchlightModel(string originalName = null, string[] aliases = null)
        {
            OriginalName = originalName;
            Aliases = aliases ?? new string[] {};
        }

        /// <summary>
        /// The underlying name for this model in the data store
        /// </summary>
        public string OriginalName { get; set; }


        /// <summary>
        /// If Searchlight should recognize this table by any other aliases, list them here
        /// </summary>
        public string[] Aliases { get; set; }
    }
}
