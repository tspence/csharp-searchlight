
namespace Searchlight
{
    public class UnterminatedValueException : SearchlightException
    {
        public UnterminatedValueException(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
