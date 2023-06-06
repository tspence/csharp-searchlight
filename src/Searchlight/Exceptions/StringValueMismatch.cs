using System;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// This error is thrown by the SQL rendering engine if it expects a string
    /// for a LIKE statement and gets something other than a string
    /// </summary>
    public class StringValueMismatch : Exception
    {
        /// <summary>
        /// The value that was expected to be a string
        /// </summary>
        public object RawValue { get; internal set; }
        
        /// <summary>
        /// A readable error message
        /// </summary>
        public string ErrorMessage
        {
            get => $"The value {RawValue} for a SQL LIKE statement was not a string.  This should have been caught by the parser.";
        }
    }
}