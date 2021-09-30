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
        public string ErrorMessage { get; internal set; } = 
            "The SearchlightField attribute was added to a column that has a name conflict with an existing column. This could be a " +
            "conflicting alias or a conflicting name. Please ensure that all field names and aliases are unique.";
    }
}