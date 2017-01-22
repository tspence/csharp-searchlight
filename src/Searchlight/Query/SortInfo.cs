using Searchlight.Parsing;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents one sort statement for a parsed query
    /// </summary>
    public class SortInfo
    {
        /// <summary>
        /// The field that is being sorted
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// The direction the field is sorted
        /// </summary>
        public SortDirection Direction { get; set; }
    }
}