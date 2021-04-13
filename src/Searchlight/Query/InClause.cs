using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    public class InClause : BaseClause
    {
        /// <summary>
        /// The field to test
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// The list of values to test against
        /// </summary>
        public List<object> Values { get; set; }
    }
}
