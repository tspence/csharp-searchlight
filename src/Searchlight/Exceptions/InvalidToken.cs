using System.Collections.Generic;

namespace Searchlight
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class InvalidToken : SearchlightException
    {
        public InvalidToken(string badToken, string[] expectedTokens, string originalFilter)
            : base(originalFilter)
        {
            BadToken = badToken;
            ExpectedTokens = expectedTokens;
        }

        public string BadToken { get; set; }
        public string[] ExpectedTokens { get; set; }
    }
}
