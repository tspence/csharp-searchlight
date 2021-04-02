
namespace Searchlight
{
    /// <summary>
    /// Indicates the query was unbalanced - there were clauses that were opened but not closed
    /// </summary>
    public class OpenClause : SearchlightException
    {
        /// <summary>
        /// Throw this exception if the query had a mismatch in the number of open parenthesis vs close parenthesis
        /// </summary>
        /// <param name="originalFilter"></param>
        public OpenClause(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
