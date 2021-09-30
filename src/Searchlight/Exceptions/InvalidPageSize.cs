using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Exceptions
{
    /// <summary>
    /// The query specified a page size that is not valid.
    ///
    /// Example: `?pageSize=-1`
    /// </summary>
    public class InvalidPageSize : SearchlightException
    {
        public string PageSize { get; internal set; }
        public string ErrorMessage { get; internal set; } = 
            "The query specified a page size that is not valid.";
    }
}
