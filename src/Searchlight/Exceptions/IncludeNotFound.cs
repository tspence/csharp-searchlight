
namespace Searchlight
{
    /// <summary>
    /// The query specified an include command or flag that was not recognized.  See the list of known
    /// includes for a full list of options.
    /// </summary>
    public class IncludeNotFound : SearchlightException
    {
        public string OriginalInclude { get; internal set; }
        public string IncludeName { get; internal set; }
        public string[] KnownIncludes { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The query specified an include command or flag that was not recognized. See the list of known includes for a full list of options.";
    }
}
