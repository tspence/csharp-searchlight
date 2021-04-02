
namespace Searchlight
{
    public class TooManyParameters : SearchlightException
    {
        public int MaximumParameterCount { get; set; }
        /// <summary>
        /// SETEC ASTRONOMY
        /// </summary>
        /// <param name="originalFilter"></param>
        public TooManyParameters(int maxParams, string originalFilter)
            : base(originalFilter)
        {
            this.MaximumParameterCount = maxParams;
        }
    }
}
