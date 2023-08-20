using Searchlight.Nesting;
using Searchlight.Parsing;
using Searchlight.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Searchlight.Exceptions;
using Searchlight.Expressions;

namespace Searchlight
{
    /// <summary>
    /// Represents a data source used to validate queries
    /// </summary>
    public class DataSource
    {
        /// <summary>
        /// The engine to use for related tables
        /// </summary>
        public SearchlightEngine Engine { get; set; }

        /// <summary>
        /// The externally visible name of this collection or table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The internal type of the model queried by this source
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        /// The field name of the default sort field, if none are specified.
        /// This is necessary to ensure reliable pagination.
        /// </summary>
        public string DefaultSort { get; set; }

        /// <summary>
        /// This function produces a list of optional commands that can be specified in the $include parameter
        /// </summary>
        public List<ICommand> Commands { get; set; }

        /// <summary>
        /// The list of flags that can be specified in the $include parameter
        /// </summary>
        public List<SearchlightFlag> Flags { get; set; }

        /// <summary>
        /// Some data sources can only handle a specified number of parameters.
        /// If set at the data source level, this overrides the value set on the SearchlightEngine object.
        /// </summary>
        public int? MaximumParameters { get; set; }

        internal readonly List<string> _knownIncludes = new List<string>();
        internal readonly Dictionary<string, object> _includeDict = new Dictionary<string, object>();
        internal readonly Dictionary<string, ColumnInfo> _fieldDict = new Dictionary<string, ColumnInfo>();
        internal readonly List<ColumnInfo> _columns = new List<ColumnInfo>();

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public DataSource WithColumn(string columnName, Type columnType)
        {
            return WithRenamingColumn(columnName, columnName, null, columnType);
        }

        /// <summary>
        /// Add a column to this definition
        /// </summary>
        /// <param name="filterName"></param>
        /// <param name="columnName"></param>
        /// <param name="aliases"></param>
        /// <param name="columnType"></param>
        /// <returns></returns>
        public DataSource WithRenamingColumn(string filterName, string columnName, string[] aliases, Type columnType)
        {
            var columnInfo = new ColumnInfo(filterName, columnName, aliases, columnType);
            _columns.Add(columnInfo);

            // Allow the API caller to either specify either the model name or one of the aliases
            AddName(filterName, columnInfo);
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    AddName(alias, columnInfo);
                }
            }

            return this;
        }

        private void AddName(string name, ColumnInfo col)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var upperName = name.Trim().ToUpperInvariant();
            if (_fieldDict.TryGetValue(upperName, out var existing))
            {
                throw new DuplicateName
                {
                    Table = this.TableName,
                    ExistingColumn = existing.FieldName,
                    ConflictingColumn = col.FieldName,
                    ConflictingName = upperName,
                };
            }

            _fieldDict[upperName] = col;
        }

        private void AddInclude(string name, object incl)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var upperName = name.Trim().ToUpperInvariant();
            if (_includeDict.ContainsKey(upperName))
            {
                throw new DuplicateInclude
                {
                    Table = this.TableName,
                    ConflictingIncludeName = upperName
                };
            }

            _includeDict[upperName] = incl;
        }

        /// <summary>
        /// Gets the list of columns for this data source
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ColumnInfo> GetColumnDefinitions()
        {
            return _columns;
        }

        /// <summary>
        /// Gets the list of column names for this data source
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ColumnNames()
        {
            return _fieldDict.Keys;
        }

        /// <summary>
        /// Identify a single column by its token
        /// </summary>
        /// <param name="filterToken"></param>
        /// <returns></returns>
        public ColumnInfo IdentifyColumn(string filterToken)
        {
            if (string.IsNullOrWhiteSpace(filterToken)) return null;
            _fieldDict.TryGetValue(filterToken.ToUpper(), out ColumnInfo ci);
            return ci;
        }


        /// <summary>
        /// Create a searchlight data source based on an in-memory collection
        /// </summary>
        /// <param name="engine">The engine containing all child tables for this data source; null if this is a standalone table</param>
        /// <param name="modelType">The type of the model for this data source</param>
        /// <param name="mode">The parsing mode for fields on this class</param>
        /// <returns></returns>
        public static DataSource Create(SearchlightEngine engine, Type modelType, AttributeMode mode)
        {
            var src = new DataSource
            {
                Engine = engine,
                Commands = new List<ICommand>(),
                Flags = modelType.GetCustomAttributes<SearchlightFlag>().ToList()
            };
            var modelAttribute = modelType.GetCustomAttribute<SearchlightModel>();
            src.ModelType = modelType;
            if (modelAttribute == null && mode == AttributeMode.Strict)
            {
                throw new NonSearchlightModel { ModelTypeName = modelType.Name };
            }
            if (modelAttribute != null)
            {
                src.TableName = modelAttribute.OriginalName ?? modelType.Name;
                src.MaximumParameters = modelAttribute.MaximumParameters;
                src.DefaultSort = modelAttribute.DefaultSort ?? modelType.GetDefaultMembers().FirstOrDefault()?.Name;
            }
            else
            {
                src.TableName = modelType.Name;
            }
            foreach (var pi in modelType.GetProperties())
            {
                // Searchlight does not support list/array element syntax
                if (pi.GetIndexParameters().Length == 0)
                {
                    if (mode == AttributeMode.Loose)
                    {
                        src.WithColumn(pi.Name, pi.PropertyType);
                    }
                    else
                    {
                        var filter = pi.GetCustomAttributes<SearchlightField>().FirstOrDefault();
                        if (filter != null)
                        {
                            // If this is a renaming column, add it appropriately
                            Type t = filter.FieldType ?? pi.PropertyType;
                            src.WithRenamingColumn(pi.Name, filter.OriginalName ?? pi.Name,
                                filter.Aliases ?? Array.Empty<string>(), t);
                        }

                        var collection = pi.GetCustomAttributes<SearchlightCollection>().FirstOrDefault();
                        if (collection != null)
                        {
                            src.Commands.Add(new CollectionCommand(src, collection, pi));
                        }
                    }
                }
            }
            
            // default sort cannot be null and must be a valid column
            if (src.DefaultSort != null)
            {
                try
                {
                    var sort = SyntaxParser.ParseOrderBy(src, src.DefaultSort);
                    if (sort.Count == 0)
                    {
                        throw new InvalidDefaultSort
                            {Table = src.TableName, DefaultSort = src.DefaultSort};
                    }
                }
                catch
                {
                    throw new InvalidDefaultSort
                        {Table = src.TableName, DefaultSort = src.DefaultSort};
                }
            }
            else
            {
                throw new InvalidDefaultSort
                    {Table = src.TableName, DefaultSort = "NULL"};
            }

            // Calculate the list of known "include" commands
            foreach (var cmd in src.Commands)
            {
                src.AddInclude(cmd.GetName(), cmd);
                src._knownIncludes.Add(cmd.GetName());
                foreach (var name in cmd.GetAliases())
                {
                    src.AddInclude(name, cmd);
                }
            }
            foreach (var flag in src.Flags)
            {
                src.AddInclude(flag.Name, flag);
                src._knownIncludes.Add(flag.Name);
                if (flag.Aliases != null)
                {
                    foreach (var alias in flag.Aliases)
                    {
                        src.AddInclude(alias, flag);
                    }
                }
            }

            return src;
        }

        /// <summary>
        /// Shortcut function to parse a filter string
        /// </summary>
        /// <param name="filter">The filter statement in Searchlight query language</param>
        /// <returns>The syntax tree if parsed successfully, or an exception</returns>
        public SyntaxTree ParseFilter(string filter)
        {
            return SyntaxParser.Parse(this, filter);
        }

        /// <summary>
        /// Parse a complete fetch request
        /// </summary>
        /// <param name="fetchRequest">The fetch request</param>
        /// <returns>The syntax tree if parsed successfully, or an exception</returns>
        public SyntaxTree Parse(FetchRequest fetchRequest)
        {
            return SyntaxParser.Parse(this, fetchRequest);
        }

        /// <summary>
        /// Shortcut function to parse an order-by statement
        /// </summary>
        /// <param name="orderBy">The order-by statement to parse</param>
        /// <returns>The list of sort instructions if parsed successfully, or an exception</returns>
        public List<SortInfo> ParseOrderBy(string orderBy)
        {
            return SyntaxParser.ParseOrderBy(this, orderBy);
        }
    }
}