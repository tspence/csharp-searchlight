namespace Searchlight.Exceptions
{
    /// <summary>
    /// Exception to be thrown if the SearchlightEngine was configured incorrectly
    /// </summary>
    public class InvalidEngineSetting : SearchlightException
    {
        public string OriginalFilter { get; internal set; }

        /// <summary>
        /// Fields that are missing or incorrect
        /// </summary>
        public string[] Fields { get; set; }

        public string ErrorMessage =>
            $"These fields are either missing or are set incorrectly: {string.Join(",", Fields)}";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fields"></param>
        public InvalidEngineSetting(params string[] fields)
        {
            Fields = fields;
        }
    }
}