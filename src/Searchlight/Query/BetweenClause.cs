using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    /// <summary>
    /// A query criteria of the form "X between low and high"
    /// </summary>
    public class BetweenClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public ColumnInfo Column { get; set; }

        /// <summary>
        /// Lower value in the between test
        /// </summary>
        public object LowerValue { get; set; }

        /// <summary>
        /// Upper value in the between test
        /// </summary>
        public object UpperValue { get; set; }
    }
}
