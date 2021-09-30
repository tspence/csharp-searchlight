
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
        public string ErrorMessage { get; internal set;  } = 
            "The filter specified a field criteria that had a data type mismatch and could not be executed. " +
            "Example: `(numberOfDays > 'Alice')` where `numberOfDays` is an integer";
    }
}
