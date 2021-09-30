
namespace Searchlight
{
    /// <summary>
    /// A filter statement contained an unterminated string.  An opening apostrophe was observed but the remainder
    /// of the string did not contain a closing apostrophe.
    ///
    /// Example: `(name eq 'Alice`
    /// </summary>
    public class UnterminatedString : SearchlightException
    {
        public string OriginalFilter { get; internal set; }
        public string Token { get; internal set; }

        public string ErrorMessage { get; internal set; } =
            "A filter statement contained an unterminated string. An opening apostrophe was observed but " +
            "the remainder of the string did not contain a closing apostrophe. `(name eq 'Alice`";
    }
}
