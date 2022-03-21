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
    }
}