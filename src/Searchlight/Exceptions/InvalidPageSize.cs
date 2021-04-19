using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Exceptions
{
    public class InvalidPageSize : SearchlightException
    {
        public string PageSize { get; set; }
    }
}
