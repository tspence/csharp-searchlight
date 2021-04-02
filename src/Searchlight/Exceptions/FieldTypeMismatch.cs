﻿
namespace Searchlight
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class FieldTypeMismatch : SearchlightException
    {
        public FieldTypeMismatch(string fieldName, string fieldType, string fieldValue, string originalFilter)
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