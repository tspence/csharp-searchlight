
namespace Searchlight.Exceptions
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class FieldNameException : SearchlightException
    {
        public FieldNameException(string fieldName, string originalFilter)
            : base(originalFilter)
        {
            FieldName = fieldName;
        }

        public string FieldName { get; set; }
    }
}
