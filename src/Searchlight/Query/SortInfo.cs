namespace Searchlight.Query
{
    /// <summary>
    /// Represents one sort statement for a parsed query
    /// </summary>
    public class SortInfo
    {
        /// <summary>
        /// The fieldname of the field that is being sorted
        /// </summary>
        public string Fieldname { get; set; }

        /// <summary>
        /// The direction the field is sorted
        /// </summary>
        public SortDirection Direction { get; set; }
    }
}