namespace Searchlight
{
    /// <summary>
    /// The filter ended with a conjunction but no elements after it.
    ///
    /// Example: `(name eq 'Alice') AND`
    /// </summary>
    public class TrailingConjunction : SearchlightException
    {
        public string OriginalFilter { get; internal set; }

        public string ErrorMessage { get; internal set; } = 
            "The filter ended with a conjunction but no elements after it. " +
            "Example: `(name eq 'Alice') AND`";
    };
}