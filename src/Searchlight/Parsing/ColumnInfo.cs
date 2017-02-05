using System;
using System.Reflection;

namespace Searchlight.Parsing
{
    /// <summary>
    /// Represents a column that can be used in the database
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// Simple constructor for a column and a type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        public ColumnInfo(string fieldName, Type fieldType)
        {
            FieldName = fieldName;
            DatabaseColumn = fieldName;
            FieldType = fieldType;
        }

        /// <summary>
        /// Constructor for a column that should be renamed in the parser
        /// </summary>
        /// <param name="filterName">The name supplied in the raw filter</param>
        /// <param name="columnName">The name of the column in the database</param>
        /// <param name="columnType">The raw type of the column in the database</param>
        /// <param name="columnType">The user-mediated enum type that will be converted</param>
        public ColumnInfo(string filterName, string columnName, Type columnType, Type enumType)
        {
            FieldName = filterName;
            DatabaseColumn = columnName;
            FieldType = columnType;
            EnumType = enumType;
        }

        /// <summary>
        /// This is the name of the field that the user provides in the filter text
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// This is the name of the field as it is emitted into SQL expressions
        /// </summary>
        public string DatabaseColumn { get; private set; }

        /// <summary>
        /// When the user compares a field to a parameter, the parameter must be convertable to this type
        /// </summary>
        public Type FieldType { get; private set; }

        /// <summary>
        /// If the end user thinks of this column as an enum, this is the type it should be parsed as
        /// </summary>
        public Type EnumType { get; private set; }

        /// <summary>
        /// Reflection property for this column; useful for retrieving or saving to an object
        /// </summary>
        public PropertyInfo Property { get; private set; }
    }
}