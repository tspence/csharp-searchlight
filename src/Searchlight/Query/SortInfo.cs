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

        /// <summary>
        /// Convenience to return the abbreviated string for directions
        /// </summary>
        /// <returns></returns>
        public string DirectionStr()
        {
            return Direction == SortDirection.Ascending
                ? StringConstants.ASCENDING_ABBR
                : StringConstants.DESCENDING_ABBR;
        }
    }
}