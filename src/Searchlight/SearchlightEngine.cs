using System;
using System.Collections.Generic;
using Searchlight.Query;

namespace Searchlight
{
    public class SearchlightEngine
    {
        private readonly Dictionary<string, DataSource> _dictionary = new Dictionary<string, DataSource>();
            
        /// <summary>
        /// Adds a new class to the engine
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SearchlightEngine AddClass(Type type)
        {
            var ds = DataSource.Create(this, type, AttributeMode.Strict);
            _dictionary.Add(type.Name, ds);
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
            return source?.Parse(request);
        }

        /// <summary>
        /// Find a data source by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public DataSource FindTable(string name)
        {
            return _dictionary.TryGetValue(name, out var source) ? source : null;
        }
    }
}