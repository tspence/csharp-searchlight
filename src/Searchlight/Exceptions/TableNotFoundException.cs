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