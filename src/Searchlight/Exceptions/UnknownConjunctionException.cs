using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight
{
    /// <summary>
    /// Throw this exception if the user specified something other than AND or OR
    /// </summary>
    public class UnknownConjunctionException : SearchlightException
    {
        public string UnknownConjunction { get; set; }

        /// <summary>
        /// Throw this exception if the user specified something other than AND or OR
        /// </summary>
        /// <param name="originalFilter"></param>
        public UnknownConjunctionException(string originalFilter, string badConjunction)
            : base(originalFilter)
        {
            UnknownConjunction = badConjunction;
        }
    }
}
