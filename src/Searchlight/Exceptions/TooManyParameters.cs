
namespace Searchlight
{
    /// <summary>
    /// SETEC ASTRONOMY
    ///
    /// Example: A query with 200 parameters when the data source has a maximum parameterized value set to 150
    /// </summary>
    public class TooManyParameters : SearchlightException
    {
        public string OriginalFilter { get; internal set; }
        public int MaximumParameterCount { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The request exceeds the maximum parameter count. Example, a query with 200" +
            "parameters when the data source has a maximum parameterized value set to 150";
    }
}
