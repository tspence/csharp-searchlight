namespace Searchlight
{
    public class DuplicateName : SearchlightException
    {
        public string ConflictingName { get; set; }
        public string ExistingColumn { get; set; }
        public string ConflictingColumn { get; set; }
    }
}