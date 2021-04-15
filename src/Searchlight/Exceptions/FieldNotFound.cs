
namespace Searchlight
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class FieldNotFound : SearchlightException
    {
        public FieldNotFound(string fieldName, string[] knownFields, string originalFilter)
        {
            this.OriginalFilter = originalFilter;
            FieldName = fieldName;
            KnownFields = knownFields;
        }

        public string OriginalFilter { get; set; }

        public string FieldName { get; set; }

        public string[] KnownFields { get; set; }
    }
}
