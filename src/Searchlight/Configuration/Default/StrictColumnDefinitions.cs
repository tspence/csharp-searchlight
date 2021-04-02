using System;
using System.Linq;
using System.Reflection;
using Searchlight;

namespace Searchlight.Configuration.Default
{
    /// <summary>
    /// This class requires that you must define a SearchlightField annotation on every field that is queryable
    /// for your model.
    /// </summary>
    public class StrictColumnDefinitions : CustomColumnDefinition
    {
        /// <summary>
        /// Constructs a list of column definitions based on an API model rather than a database entity.
        /// Validates that all properties used in the filter must use the "Filterable" annotation.
        /// Supports column renaming from the model's variable name to the database column.
        /// </summary>
        /// <param name="modelType"></param>
        public StrictColumnDefinitions(Type modelType)
            : base()
        {
            // Find all properties on this type
            foreach (var pi in modelType.GetProperties())
            {

                // Member variables that are lists or arrays can't be transformed into SQL
                if (pi.GetIndexParameters().Length == 0)
                {

                    // Is there a "filterable" annotation on this property?
                    var filter = pi.GetCustomAttributes<SearchlightField>().FirstOrDefault();
                    if (filter != null)
                    {

                        // If this is a renaming column, add it appropriately
                        Type t = filter.FieldType ?? pi.PropertyType;
                        WithRenamingColumn(pi.Name, filter.OriginalName ?? pi.Name, filter.Aliases ?? new string[] { }, t, filter.EnumType);
                    }
                }
            }
        }
    }
}