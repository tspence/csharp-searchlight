using Searchlight.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Query
{
    public class TextClause : BaseClause
    {
        /// <summary>
        /// The field being tested
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The text operation to use - e.g. StartsWith, Contains, EndsWith, or Like
        /// </summary>
        public OperationType Operation { get; set; }

        /// <summary>
        /// Text to test against 
        /// </summary>
        public string StartingText { get; set; }
    }
}
