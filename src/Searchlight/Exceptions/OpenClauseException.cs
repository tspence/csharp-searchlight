
namespace Searchlight
{
    /// <summary>
    /// Indicates the query was unbalanced - there were clauses that were opened but not closed
    /// </summary>
    public class OpenClauseException : SearchlightException
    {
        /// <summary>
        /// Throw this exception if the query had a mismatch in the number of open parenthesis vs close parenthesis
        /// </summary>
        /// <param name="originalFilter"></param>
        public OpenClauseException(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
