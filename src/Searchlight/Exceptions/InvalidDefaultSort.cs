using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Exceptions
{
    public class InvalidDefaultSort : SearchlightException
    {
        public string DefaultSort { get; set; }
    }
}