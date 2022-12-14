#pragma warning disable CS1591
namespace Searchlight
{
    /// <summary>
    /// The filter specified a field name that was not known.  Check the list of known fields to see if the filter
    /// contains a typographical error.
    ///
    /// Example: `(someUnknownField eq 'alice')` where `someUnknownField` does not exist.
    /// </summary>
    public class FieldNotFound : SearchlightException
    {
        public string OriginalFilter { get; internal set; }

        public string FieldName { get; internal set; }

        public string[] KnownFields { get; internal set; }

        public string ErrorMessage
        {
            get =>
                $"The query filter, {OriginalFilter}, had a field name that was not known. Check the list of known fields to see if the filter " +
                $"contains a typographical error: {string.Join(", ", KnownFields)}";
        }
    }
}