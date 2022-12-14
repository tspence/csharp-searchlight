
#pragma warning disable CS1591
namespace Searchlight
{
    /// <summary>
    /// The filter specified a field criteria that had a data type mismatch and could not be executed.
    ///
    /// Example: `(numberOfDays > 'Alice')` where `numberOfDays` is an integer
    /// </summary>
    public class FieldTypeMismatch : SearchlightException
    {
        public string OriginalFilter { get; internal set; }
        public string FieldName { get; internal set; }
        public string FieldValue { get; internal set; }
        public string FieldType { get; internal set; }
        public string ErrorMessage
        {
            get =>
                $"The filter {OriginalFilter} specified a field criteria that had a data type mismatch and could not be executed. " +
                $"Please ensure your filter is of type {FieldType}";
        }
    }
}
