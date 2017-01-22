using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Searchlight.Annotations
{
    /// <summary>
    /// Represents a field that is permitted to be used as a filter or sort-by column in the SafeParser
    /// </summary>
    public class Filterable : Attribute
    {
        /// <summary>
        /// Information about the name of the field and its type in the database, if that type is different from the type the user sees.
        /// You can specify the type the user sees as well as the type as it is stored in the database.
        /// </summary>
        /// <param name="rename"></param>
        /// <param name="fieldType"></param>
        /// <param name="enumType">If the field is an enum, the field should be parsed as an enum using (enumType) and converted to (fieldtype) before querying</param>
        public Filterable(string rename = null, Type fieldType = null, Type enumType = null)
        {
            Rename = rename;
            FieldType = fieldType;
            EnumType = enumType;
        }

        /// <summary>
        /// If this column is named differently in the API, this is the official SQL name of the column
        /// </summary>
        public string Rename { get; set; }

        /// <summary>
        /// If this field is a different type in the database, this is the actual field type in the DB
        /// </summary>
        public Type FieldType { get; set; }

        /// <summary>
        /// If this field is presented to the user as an enum, use this source enum to parse the value before converting to fieldType for querying
        /// </summary>
        public Type EnumType { get; set; }
    }
}
