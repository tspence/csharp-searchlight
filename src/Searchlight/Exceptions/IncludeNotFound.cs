
namespace Searchlight
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class IncludeNotFound : SearchlightException
    {
        /// <summary>
        /// The requested list of includes
        /// </summary>
        public string OriginalInclude { get; set; }

        /// <summary>
        /// The include that was not found
        /// </summary>
        public string IncludeName { get; set; }

        /// <summary>
        /// The list of known includes for this table
        /// </summary>
        public string[] KnownIncludes { get; set; }
    }
}
