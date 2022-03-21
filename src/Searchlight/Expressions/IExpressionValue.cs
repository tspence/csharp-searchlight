namespace Searchlight.Expressions
{
    /// <summary>
    /// Represents the right-hand side value of an expression
    /// </summary>
    public interface IExpressionValue
    {
        /// <summary>
        /// Retrieve the value for this object
        /// </summary>
        /// <returns></returns>
        object GetValue();
    }
}