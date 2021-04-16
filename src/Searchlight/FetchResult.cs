using System.Collections.Generic;
using System.Linq;

namespace Searchlight
{
    /// <summary>
    /// Represents a fetch result
    /// </summary>
    /// <typeparam name="T">The type of object to return</typeparam>
    public class FetchResult<T>
    {
        /// <summary>
        /// The total number of rows matching the filter.  If unknown, returns null.
        /// </summary>
        public int? totalCount { get; set; }

        /// <summary>
        /// If the original request was submitted using Page Size-based pagination, contains the page size for this query.  Null otherwise.
        /// </summary>
        public int? pageSize { get; set; }

        /// <summary>
        /// If the original request was submitted using Page Size-based pagination, contains the page number of this current result.  Null otherwise.
        ///
        /// To get the next page of results, make a new request with the same pageSize but add one to the pageNumber.
        /// </summary>
        public int? pageNumber { get; set; }

        /// <summary>
        /// The paginated and filtered list of records matching the parameters you supplied.
        /// </summary>
        public T[] records { get; set; }

        /// <summary>
        /// Simple Constructor
        /// </summary>
        public FetchResult()
        {
            records = null;
            totalCount = null;
        }

        /// <summary>
        /// Construct results for a searchlight request and return information about pagination
        /// </summary>
        /// <param name="request">The original request</param>
        /// <param name="records">The paginated subsection of records returned from the query</param>
        /// <param name="totalCount">The total number of records returned from the query, if known.  If it is computationally problematic to calculate this number, leave it null.</param>
        public FetchResult(FetchRequest request, IEnumerable<T> records, int? totalCount)
        {
            this.pageSize = request.pageSize;
            this.pageNumber = request.pageNumber;
            this.totalCount = totalCount;
            this.records = records.ToArray();
        }
    }
}
