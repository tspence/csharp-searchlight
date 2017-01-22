using System;
using System.Reflection;
using Searchlight.Annotations;
using System.Linq;

namespace Searchlight.Configuration.Default
{
    /// <summary>
    /// Represents a class that parses a model and looks for "Filterable" annotations on fields.
    /// </summary>
    public class ModelColumnDefinitions : CustomColumnDefinition
    {
        /// <summary>
        /// Constructs a list of column definitions based on an API model rather than a database entity.
        /// Only works with properties that use the "Filterable" annotation.
        /// Supports column renaming from the model's variable name to the database column.
        /// </summary>
        /// <param name="modelType"></param>
        public ModelColumnDefinitions(Type modelType)
            : base()
        {
            // Find all properties on this type
            foreach (var pi in modelType.GetProperties()) {

                // Member variables that are lists or arrays can't be transformed into SQL
                if (pi.GetIndexParameters().Length == 0) {

                    // Is there a "filterable" annotation on this property?
                    var filter = pi.GetCustomAttributes<Filterable>().FirstOrDefault();
                    if (filter != null) {

                        // If this is a renaming column, add it appropriately
                        Type t = filter.FieldType ?? pi.PropertyType;
                        if (String.IsNullOrWhiteSpace(filter.Rename)) {
                            WithColumn(pi.Name, t, filter.EnumType);
                        } else {
                            WithRenamingColumn(pi.Name, filter.Rename, t, filter.EnumType);
                        }
                    }
                }
            }
        }
    }
}