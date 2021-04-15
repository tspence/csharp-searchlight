
namespace Searchlight
{
    public class UnterminatedString : SearchlightException
    {
        public UnterminatedString(string token, string originalFilter)
        {
            Token = token;
            OriginalFilter = originalFilter;
        }

        public string OriginalFilter { get; set; }

        public string Token { get; private set; }
    }
}
