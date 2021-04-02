
namespace Searchlight
{
    /// <summary>
    /// User ended the filter with a conjunction that required additional elements after it
    /// </summary>
    public class TrailingConjunction : SearchlightException
    {
        /// <summary>
        /// Inform the user that a conjunction requires a following clause
        /// </summary>
        /// <param name="originalFilter"></param>
        public TrailingConjunction(string originalFilter)
            : base(originalFilter)
        {
        }
    }
}
