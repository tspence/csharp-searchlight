
namespace Searchlight
{
    public class UnterminatedString : SearchlightException
    {
        public UnterminatedString(string token, string originalFilter)
            : base(originalFilter)
        {
            Token = token;
        }

        public string Token { get; private set; }
    }
}
