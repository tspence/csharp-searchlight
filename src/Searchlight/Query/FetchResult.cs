using System.Collections.Generic;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents a fetch result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FetchResult<T>
    {
        /// <summary>
        /// The number of rows returned by your query, prior to pagination.  
        /// </summary>
        public int? count { get; set; }

        /// <summary>
        /// The paginated and filtered list of records matching the parameters you supplied.
        /// </summary>
        public List<T> value { get; set; }

        /// <summary>
        /// The link to the next page of results
        /// </summary>
        public string nextLink { get; set; }

        /// <summary>
        /// Simple Constructor
        /// </summary>
        public FetchResult()
        {
            value = new List<T>();
            count = 0;
        }

        /// <summary>
        /// Construct this from a different FetchResult, but maintain the count
        /// </summary>
        public FetchResult(int? originalRowCount, List<T> newlist)
        {
            this.count = originalRowCount ?? newlist.Count;
            this.value = newlist;
        }
    }
}
