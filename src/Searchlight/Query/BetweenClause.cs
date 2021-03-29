using Searchlight.Parsing;

namespace Searchlight.Query
{
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
