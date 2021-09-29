using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Searchlight.Query;
using Searchlight.Exceptions;

namespace Searchlight
{
    public class SearchlightEngine
    {
        private readonly Dictionary<string, DataSource> _dictionary = new Dictionary<string, DataSource>();

        /// <summary>
        /// Captures all known model errors
        /// </summary>
        public List<SearchlightException> ModelErrors { get; } = new List<SearchlightException>();

        /// <summary>
        /// Represents the maximum size of a single page
        /// </summary>
        public static int MaximumPageSize { get; } = 1000;

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
        /// Parse this fetch request using a data source defined within this engine.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public SyntaxTree Parse(FetchRequest request)
        {
            var source = FindTable(request.table);
            if (request.pageSize == null)
            {
                request.pageSize = MaximumPageSize;
            }
            if (request.pageSize > MaximumPageSize)
            {
                throw new InvalidPageSize { PageSize = $"larger than the allowed maximum pageSize, {MaximumPageSize}"};
            }
            return source?.Parse(request);
        }

        /// <summary>
        /// Find a data source by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataSource FindTable(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return null;
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