namespace Searchlight
{
    public class DuplicateName : SearchlightException
    {
        /// <summary>
        /// This is the table that had the conflicting field name
        /// </summary>
        public string Table { get; set; }
        public string ConflictingName { get; set; }
        public string ExistingColumn { get; set; }
        public string ConflictingColumn { get; set; }
    }
}