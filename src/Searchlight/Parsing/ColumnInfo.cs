using System;

namespace Searchlight.Parsing
{
    /// <summary>
    /// Represents a column that can be used in the database
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// Constructor for a column that should be renamed in the parser
        /// </summary>
        /// <param name="filterName">The name supplied in the raw filter</param>
        /// <param name="columnName">The name of the column in the database</param>
        /// <param name="aliases">If this field is known by other names, list them here</param>
        /// <param name="columnType">The raw type of the column in the database</param>
        /// <param name="enumType">The type of the enum that the column is mapped to</param>
        public ColumnInfo(string filterName, string columnName, string[] aliases, Type columnType, Type enumType)
        {
            FieldName = filterName;
            OriginalName = columnName;
            FieldType = columnType;
            Aliases = aliases;
            if (enumType != null && !enumType.IsEnum)
            {
                throw new ArgumentException("Must specify and enum type", nameof(enumType));
            }

            EnumType = enumType;
        }

        /// <summary>
        /// This is the name of the field that the user provides in the filter text
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// This is the name of the field that the user provides in the filter text
        /// </summary>
        public string[] Aliases { get; private set; }

        /// <summary>
        /// This is the name of the field as it is emitted into SQL expressions
        /// </summary>
        public string OriginalName { get; private set; }

        /// <summary>
        /// When the user compares a field to a parameter, the parameter must be convertable to this type
        /// </summary>
        public Type FieldType { get; private set; }
        
        /// <summary>
        /// When the user specifies a field to be an enum, the parameter must be mapped by this type
        /// ex. The field type is int and enum is CarType { Sedan = 0, SUV = 1 } so "Sedan" should translate to 0
        /// </summary>
        public Type EnumType { get; private set; }
    }
}