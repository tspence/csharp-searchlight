namespace Searchlight.Expressions
{
    /// <summary>
    /// Represents a constant
    /// </summary>
    public class ConstantValue : IExpressionValue
    {
        /// <summary>
        /// The constant
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Interface
        /// </summary>
        /// <returns></returns>
        public object GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Construct a constant value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConstantValue From(object value)
        {
            return new ConstantValue() { Value = value };
        }

        /// <summary>
        /// Custom string value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Value}";
        }
    }
}