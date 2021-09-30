using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string ErrorMessage { get; internal set; } = 
            "The query specified a page number that is not valid. Example: `?pageNumber=-1`";
    }
}
