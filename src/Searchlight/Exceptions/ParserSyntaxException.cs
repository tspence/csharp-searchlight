using System.Collections.Generic;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// Represents a failure in the SQL validation
    /// </summary>
    public class ParserSyntaxException : SearchlightException
    {
        public ParserSyntaxException(string badToken, IEnumerable<string> expectedTokens, string originalFilter)
            : base(originalFilter)
        {
            BadToken = badToken;
            ExpectedTokens = expectedTokens;
        }

        public string BadToken { get; set; }
        public IEnumerable<string> ExpectedTokens { get; set; }
    }
}
