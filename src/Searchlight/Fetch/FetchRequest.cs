using System;

namespace Searchlight.Query
{
    /// <summary>
    /// Represents a complex fetch request to parse using query filter guidelines from Microsoft REST standards
    /// </summary>
    public class FetchRequest
    {
        /// <summary>
        /// A list of conditions to filter objects.  For example, "ID > 123" or "IsActive = 1".
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// A list of included commands for this fetch operation.
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// For pagination: This is the maximum number of results to return.
        /// </summary>
        public int MaxResults { get; set; }

        /// <summary>
        /// For pagination: This is the index of the first result.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Sorts the resulting objects in a specific manner.
        /// </summary>
        public string SortBy { get; set; }

        #region Interface
        /// <summary>
        /// Append a field to a user-provided filter - useful for restricting a user's ability to query to a limited subset of records
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="operation"></param>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        public void AppendFilter(string fieldName, string operation, string fieldValue)
        {
            // If there's no new value to provide, skip it
            if (fieldValue == null) return;

            // Okay, there's a value to provide, let's append it
            AppendFilter(fieldName + " " + operation + " '" + fieldValue + "'");
        }

        /// <summary>
        /// Append a field to a user-provided filter - useful for restricting a user's ability to query to a limited subset of records
        /// </summary>
        /// <param name="singleFilter"></param>
        /// <returns></returns>
        public void AppendFilter(string singleFilter)
        {
            // If there's no new value to provide, skip it
            if (singleFilter == null) return;

            // Okay, there's a value to provide, let's append it
            if (String.IsNullOrWhiteSpace(Filter))
            {
                Filter = singleFilter;
            }
            else
            {
                Filter = "(" + Filter + ") AND " + singleFilter;
            }
        }

        /// <summary>
        /// Append a field to a user-provided filter - useful for restricting a user's ability to query to a limited subset of records
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="operation"></param>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        public void AppendFilter(string fieldName, string operation, int? fieldValue)
        {
            // If there's no new value to provide, skip it
            if (fieldValue == null) return;

            // Okay, there's a value to provide, let's append it
            AppendFilter(fieldName + " " + operation + " " + fieldValue.Value.ToString());
        }
        #endregion
    }
}
