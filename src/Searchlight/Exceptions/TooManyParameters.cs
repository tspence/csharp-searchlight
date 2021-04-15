
namespace Searchlight
{
    public class TooManyParameters : SearchlightException
    {
        /// <summary>
        /// SETEC ASTRONOMY
        /// </summary>
        /// <param name="originalFilter"></param>
        public TooManyParameters(int maxParams, string originalFilter)
        {
            MaximumParameterCount = maxParams;
            OriginalFilter = originalFilter;
        }

        public string OriginalFilter { get; set; }

        public int MaximumParameterCount { get; set; }
    }
}
