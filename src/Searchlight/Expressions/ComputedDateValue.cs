using System;

namespace Searchlight.Expressions
{
    /// <summary>
    /// Represents a date value being computed
    /// </summary>
    public class ComputedDateValue : IExpressionValue
    {
        /// <summary>
        /// The root key for this date, as defined in DEFINED_DATES
        /// </summary>
        public string Root { get; set; }

        /// <summary>
        /// An increment to apply to this computed date
        /// </summary>
        public int Offset { get; set; }
        
        /// <summary>
        /// Computation for date math
        /// </summary>
        /// <returns></returns>
        public object GetValue()
        {
            return StringConstants.DEFINED_DATES[Root]().AddDays(Offset);
        }

        public override string ToString()
        {
            if (Offset == 0)
            {
                return Root;
            }
            return $"{Root} {(Offset > 0 ? "+" : "-")} {Math.Abs(Offset)}";
        }
    }
}