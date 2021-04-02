
namespace Searchlight
{
    public class TooManyParametersException : SearchlightException
    {
        public int MaximumParameterCount { get; set; }
        /// <summary>
        /// SETEC ASTRONOMY
        /// </summary>
        /// <param name="originalFilter"></param>
        public TooManyParametersException(int maxParams, string originalFilter)
            : base(originalFilter)
        {
            this.MaximumParameterCount = maxParams;
        }
    }
}
