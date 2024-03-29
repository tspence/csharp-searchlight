﻿#pragma warning disable CS1591
namespace Searchlight
{
    public class DuplicateName : SearchlightException
    {
        /// <summary>
        /// The SearchlightField attribute was added to a column that has a name conflict with
        /// an existing column.  This could be a conflicting alias or a conflicting name.  Please
        /// ensure that all field names and aliases are unique.
        /// </summary>
        public string Table { get; internal set; }
        public string ConflictingName { get; internal set; }
        public string ExistingColumn { get; internal set; }
        public string ConflictingColumn { get; internal set; }
        public string ErrorMessage
        {
            get => $"The Searchlight model {Table} had two different included fields with the name '{ConflictingName}'.";
        }
    }
}