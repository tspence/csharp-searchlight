using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight
{
    /// <summary>
    /// Represents a flag that you can set using the "$include" parameter
    /// </summary>
    public class SearchlightFlag : Attribute
    {
        /// <summary>
        /// The name by which this flag is known
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Alternative names by which this flag can be specified in the "$include" parameter
        /// </summary>
        public string[] Aliases { get; set; }
    }
}
