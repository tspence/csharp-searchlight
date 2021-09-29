namespace Searchlight
{
    /// <summary>
    /// This query specified a model that is not present in the Searchlight engine.
    /// </summary>
    public class NonSearchlightModel : SearchlightException
    {
        public string ModelTypeName { get; internal set; }
    }
}