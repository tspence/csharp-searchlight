using System;

namespace Searchlight.Exceptions
{
    public class InvalidCollection : SearchlightException
    {
        public string TableName { get; set; }
        public string CollectionName { get; set; }
        public string CollectionErrorMessage { get; set; }
    }
}