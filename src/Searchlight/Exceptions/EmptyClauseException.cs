
namespace Searchlight.Exceptions
{
    /// <summary>
    /// Filter contained no usable filter criteria
    /// </summary>
    public class EmptyClauseException : SearchlightException
    {
        /// <summary>
        /// Throw this exception if the query did not have parseable filter criteria - 
        /// for example, if it was made up entirely of AND/OR/parenthesis clauses, or
        /// if there was an empty parenthesis clause
        /// </summary>
        /// <param name="originalFilter"></param>
        public EmptyClauseException(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
