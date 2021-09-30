
namespace Searchlight
{
    /// <summary>
    /// The filter contained an empty parenthesis with no criteria within it.
    ///
    /// Example: `(name eq Alice) or ()`
    /// </summary>
    public class EmptyClause : SearchlightException
    {
        public string OriginalFilter { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The filter contained an empty parenthesis with no criteria within it. Example: `(name eq Alice) or ()`";
    }
}
