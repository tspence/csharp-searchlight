namespace Searchlight.Exceptions
{
    /// <summary>
    /// Filter contained no usable filter criteria
    /// </summary>
    public class NoFilterCriteriaException : SearchlightException
    {
        /// <summary>
        /// Throw this exception if the query did not have any parseable filter criteria - 
        /// for example, if it was made up entirely of AND/OR/parenthesis clauses
        /// </summary>
        /// <param name="originalFilter"></param>
        public NoFilterCriteriaException(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
