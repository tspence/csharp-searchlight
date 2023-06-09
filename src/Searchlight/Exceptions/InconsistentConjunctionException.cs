namespace Searchlight.Exceptions
{
    /// <summary>
    /// Represents a compound clause that had an inconsistent use of AND / OR.
    /// Examples:
    ///  * (A and B or C) - BAD
    ///  * (A and B) or C - GOOD
    ///  * A and (B or C) - GOOD
    /// </summary>
    public class InconsistentConjunctionException : SearchlightException
    {
        
        /// <summary>
        /// The incorrect part of the query 
        /// </summary>
        public string InconsistentClause { get; internal set; }
        
        /// <summary>
        /// An error message that can be shown to a user
        /// </summary>
        public string ErrorMessage
        {
            get => $"Mixing AND and OR conjunctions in the same statement results in an imprecise query. Please use parenthesis to ensure that AND and OR clauses are kept separate. The clause '{InconsistentClause}' included both AND and OR.";
        }
    }
}