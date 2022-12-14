using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1591

namespace Searchlight.Exceptions
{
    /// <summary>
    /// The query specified a page number that is not valid.
    ///
    /// Example: `?pageNumber=-1`
    /// </summary>
    public class InvalidPageNumber : SearchlightException
    {
        public string PageNumber { get; internal set; }
        public string ErrorMessage { 
            get => $"The specified page number, {PageNumber}, is not valid. Page numbers must be nonnegative integers.";
        }
    }
}
