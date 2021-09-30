using System;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// The SearchlightCollection attribute was not correctly applied to a specified table.
    /// </summary>
    public class InvalidCollection : SearchlightException
    {
        public string TableName { get; internal set; }
        public string CollectionName { get; internal set; }
        public string CollectionErrorMessage { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The SearchlightCollection attribute was not correctly applied to a specified table.";
    }
}