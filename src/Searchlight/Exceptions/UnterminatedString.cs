
#pragma warning disable CS1591
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
        public int StartPosition { get; internal set; }
        public string ErrorMessage
        {
            get =>
                $"The query filter, {OriginalFilter}, contained an unterminated string that starts at {StartPosition} but does not have a closing apostrophe.";
        }
    }
}
