namespace Searchlight
{
    /// <summary>
    /// Represents an invalid operation
    /// </summary>
    public class InvalidOperation : SearchlightException
    {
        public InvalidOperation(string fieldName, string fieldType, string fieldValue, string fieldValueType,
            string originalFilter)
        {
            OriginalFilter = originalFilter;
            FieldName = fieldName;
            FieldType = fieldType;
            FieldValue = fieldValue;
            FieldValueType = fieldValueType;
        }
        
        public string OriginalFilter { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string FieldType { get; set; }
        public string FieldValueType { get; set; }
    }
}