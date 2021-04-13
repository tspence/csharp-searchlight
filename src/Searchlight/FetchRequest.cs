using System;

namespace Searchlight
{
    /// <summary>
    /// Represents a complex fetch request to parse using query filter guidelines from Microsoft REST standards
    /// </summary>
    public class FetchRequest
    {
        /// <summary>
        /// A list of conditions to filter objects.  For example, "ID > 123" or "IsActive = 1".
        /// </summary>
        public string filter { get; set; }

        /// <summary>
        /// For pagination: The page size to use for this query.
        /// </summary>
        public int? pageSize { get; set; }

        /// <summary>
        /// For pagination: After the records above are skipped, the maximum number of records to take.  Default is unlimited.
        /// </summary>
        public int? pageNumber { get; set; }

        /// <summary>
        /// Sorts the resulting objects by these specific fields.
        /// </summary>
        public string order { get; set; }

        /// <summary>
        /// Append a field to a user-provided filter - useful for restricting a user's ability to query to a limited subset of records
        /// </summary>
        /// <param name="appendedFilter">The new filter to append to the existing query</param>
        /// <returns></returns>
        public void Append(string appendedFilter)
        {
            // If there's no new value to provide, skip it
            if (appendedFilter == null) return;

            // Okay, there's a value to provide, let's append it
            if (String.IsNullOrWhiteSpace(filter))
            {
                filter = appendedFilter;
            }
            else
            {
                filter = $"(${filter}) AND ${appendedFilter}";
            }
        }
    }
}
