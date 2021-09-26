namespace Searchlight
{
    public class DuplicateInclude : SearchlightException
    {
        /// <summary>
        /// This is the table that had the conflicting include name
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// This name appeared in the "Include" list more than once
        /// </summary>
        public string ConflictingIncludeName { get; set; }
    }
}