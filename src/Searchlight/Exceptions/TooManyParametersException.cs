
namespace Searchlight.Exceptions
{
    public class TooManyParametersException : SearchlightException
    {
        /// <summary>
        /// SETEC ASTRONOMY
        /// </summary>
        /// <param name="originalFilter"></param>
        public TooManyParametersException(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
