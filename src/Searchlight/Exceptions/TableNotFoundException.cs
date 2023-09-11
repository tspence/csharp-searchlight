#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Searchlight.Exceptions
{
    public class TableNotFoundException : SearchlightException
    {
        public string TableName { get; set; }
        public string ErrorMessage
        {
            get => $"No table {TableName} was found.";
        }
    }
}