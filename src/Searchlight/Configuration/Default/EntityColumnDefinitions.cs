using System;
using System.Reflection;

namespace Searchlight.Configuration.Default
{
    /// <summary>
    /// Represents a class that parses a model and looks for "SearchlightField" annotations on fields.
    /// </summary>
    public class EntityColumnDefinitions : CustomColumnDefinition
    {
        /// <summary>
        /// Constructs a list of columns where the user's filter text must match the SQL column name.
        /// </summary>
        /// <param name="entityType"></param>
        public EntityColumnDefinitions(Type entityType)
            : base()
        {
            // Find all properties on this type
            foreach (var pi in entityType.GetProperties())
            {
                // TODO: Support paramters that are lists or arrays
                if (pi.GetIndexParameters().Length == 0)
                {

                    // Add to our dictionary
                    WithColumn(pi.Name, pi.PropertyType, null);
                }
            }
        }
    }
}