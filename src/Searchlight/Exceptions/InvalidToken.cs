using System.Collections.Generic;

namespace Searchlight
{
    /// <summary>
    /// The filter statement contained an unexpected token.  Examine the list of expected tokens
    /// to see what Searchlight expects to find next.
    ///
    /// Example: `(name eq Alice) date eq '2021-09-29'`
    /// 
    /// In this query, Searchlight expects to see "AND" or "OR" after the close parenthesis.
    /// </summary>
    public class InvalidToken : SearchlightException
    {
        public string OriginalFilter { get; internal set; }
        public string BadToken { get; internal set; }
        public string[] ExpectedTokens { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The filter statement contained an unexpected token. Examine the list of expected tokens to see what " +
            "Searchlight expects to find next. Example: `(name eq Alice) date eq '2021-09-29'` " +
            "In this query, Searchlight expects to see \"AND\" or \"OR\" after the close parenthesis.";
    }
}
