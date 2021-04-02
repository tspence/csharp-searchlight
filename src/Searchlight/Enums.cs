namespace Searchlight
{
    public enum ModelFieldMode {
        /// <summary>
        /// Not recommended - Allows developers to query any field on the model, even fields not tagged with the `SearchlightField` annotation.
        /// This is potentially dangerous because a developer could accidentally add a field they do not intend to expose to external partners,
        /// but it allows for more rapid development.
        /// </summary>
        Loose = 0,

        /// <summary>
        /// Recommended - Only allows developers to query against fields tagged with the `SearchlightField` annotation.
        /// </summary>
        Strict = 1,
    }
}