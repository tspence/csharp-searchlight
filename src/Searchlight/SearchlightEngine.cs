using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Searchlight.Autocomplete;
using Searchlight.Exceptions;
using Searchlight.Parsing;
using Searchlight.Query;

namespace Searchlight
{
    /// <summary>
    /// A root of compiled data sources
    /// </summary>
    public class SearchlightEngine
    {
        private readonly Dictionary<string, DataSource> _dictionary = new Dictionary<string, DataSource>();

        /// <summary>
        /// Captures all known model errors
        /// </summary>
        public List<SearchlightException> ModelErrors { get; } = new List<SearchlightException>();

        /// <summary>
        /// If the user does not specify a page size, use this value
        /// </summary>
        public int? DefaultPageSize { get; set; }
        
        /// <summary>
        /// Represents the maximum size of a single page
        /// </summary>
        public int? MaximumPageSize { get; set; }
        
        /// <summary>
        /// Set the maximum complexity of Searchlight filters at the engine level
        /// </summary>
        public int? MaximumParameters { get; set; }

        /// <summary>
        /// SQL Server: Use a single compound query that returns multiple result sets to avoid excess roundtrips.
        /// </summary>
        public bool useResultSet { get; set; } = true;

        /// <summary>
        /// SQL Server: Set this flag to true to specify `WITH (NOLOCK)` for all non-temporary tables.
        /// Can potentially improve performance when true.  If you use Read Uncommitted, this flag has no
        /// effect.
        /// </summary>
        public bool useNoLock { get; set; }

        /// <summary>
        /// SQL Server: If true, all searchlight queries begin with `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED`.
        /// Can potentially improve performance when true.  Using this flag set to true can sometimes read
        /// slightly inconsistent results.
        ///
        /// DEFAULT: True.
        /// </summary>
        public bool useReadUncommitted { get; set; } = true;

        /// <summary>
        /// SQL Server: If true, prefixes a query with `SET NOCOUNT ON`.  Can potentially improve performance 
        /// when true.
        ///
        /// DEFAULT: True.
        /// </summary>
        public bool useNoCount { get; set; } = true;

        /// <summary>
        /// Adds a new class to the engine
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SearchlightEngine AddClass(Type type)
        {
            var ds = DataSource.Create(this, type, AttributeMode.Strict);
            _dictionary.Add(type.Name, ds);
            var model = type.GetCustomAttributes<SearchlightModel>().FirstOrDefault();
            if (model?.Aliases != null)
            {
                foreach (var alias in model.Aliases)
                {
                    _dictionary.Add(alias, ds);
                }
            }
            return this;
        }

        /// <summary>
        /// Add a hand-constructed data source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public SearchlightEngine AddDataSource(DataSource source)
        {
            _dictionary.Add(source.TableName, source);
            source.Engine = this;
            return this;
        }

        /// <summary>
        /// Parse this fetch request using a data source defined within this engine.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public SyntaxTree Parse(FetchRequest request)
        {
            var source = FindTable(request.table);
            if (source == null)
            {
                throw new TableNotFoundException()
                {
                    TableName = request.table,
                };
            }
            if (request.pageSize == null)
            {
                request.pageSize = DefaultPageSize;
            }
            if (MaximumPageSize != null)
            {
                if (request.pageSize == null)
                {
                    request.pageSize = MaximumPageSize;
                }
                if (request.pageSize > MaximumPageSize)
                {
                    throw new InvalidPageSize { PageSize = $"larger than the allowed maximum pageSize, {MaximumPageSize}"};
                }
            }
            return SyntaxParser.Parse(source, request);
        }

        /// <summary>
        /// When typing in a search box in a user interface, call this function to get suggestions.
        /// </summary>
        /// <param name="table">The table being searched</param>
        /// <param name="filter">The filter statement used</param>
        /// <param name="cursorPosition">The position </param>
        /// <returns></returns>
        public CompletionList AutocompleteFilter(string table, string filter, int cursorPosition)
        {
            var source = FindTable(table);
            var completion = new CompletionList() { items = new List<CompletionItem>() };
            
            // If the user hasn't typed anything, just give them a list of fields
            if (cursorPosition == 0 || string.IsNullOrWhiteSpace(filter))
            {
                return AutocompleteFields(source, null);
            }

            // Trim the autocomplete to the cursor position
            var trimmedFilter = filter.Substring(0, cursorPosition);
            var request = new FetchRequest()
            {
                table = table,
                filter = trimmedFilter,
            };
            var syntax = SyntaxParser.TryParse(source, request);
            if (syntax.Errors != null)
            {
                foreach (var e in syntax.Errors)
                {
                    if (e is InvalidToken invalidToken)
                    {
                        foreach (var token in invalidToken.ExpectedTokens)
                        {
                            completion.items.Add(new CompletionItem()
                            {
                                label = token,
                                deprecated = false,
                                detail = null,
                                kind = CompletionItemKind.Keyword,
                            });
                        }

                        return completion;
                    }

                    if (e is FieldNotFound fieldNotFound)
                    {
                        return AutocompleteFields(source, fieldNotFound.FieldName);
                    }
                }
            }

            // Uncertain how to handle this; let's give no advice
            return completion;
        }

        private CompletionList AutocompleteFields(DataSource source, string prefix)
        {
            var completion = new CompletionList()
            {
                items = new List<CompletionItem>(),
            };
            foreach (var field in source._columns.OrderBy(c => c.FieldName))
            {
                if (prefix == null || field.FieldName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    completion.items.Add(new CompletionItem()
                    {
                        label = field.FieldName,
                        kind = CompletionItemKind.Field,
                        detail = "",
                        deprecated = false,
                    });
                }
            }

            return completion;
        }

        /// <summary>
        /// Find a data source by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataSource FindTable(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _dictionary.TryGetValue(name, out var source) ? source : null;
        }

        /// <summary>
        /// Search for all classes within this assembly that have `SearchlightModel` attributes.
        /// </summary>
        /// <param name="assembly">The assembly to search</param>
        /// <returns></returns>
        public SearchlightEngine AddAssembly(Assembly assembly)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                var annotation = type.GetCustomAttributes<SearchlightModel>().FirstOrDefault();
                if (annotation != null)
                {
                    try
                    {
                        AddClass(type);
                    }
                    catch (SearchlightException e)
                    {
                        ModelErrors.Add(e);
                    }
                }
            }
            return this;
        }
    }
}