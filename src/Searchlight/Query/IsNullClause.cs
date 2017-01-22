using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    public class IsNullClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public string FieldName { get; set; }
    }
}
