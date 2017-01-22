namespace Searchlight.Exceptions
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class FieldValueException : SearchlightException
    {
        public FieldValueException(string fieldName, string fieldType, string fieldValue, string originalFilter)
            : base(originalFilter)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            FieldValue = fieldValue;
        }

        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string FieldType { get; set; }
    }
}
