using System;
using System.Collections.Generic;
using Searchlight.Query;

namespace Searchlight
{
    public class SearchlightEngine
    {
        private Dictionary<string, DataSource> _dictionary = new Dictionary<string, DataSource>();
            
        /// <summary>
        /// Adds a new class to the engine
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SearchlightEngine AddClass(Type type)
        {
            var ds = DataSource.Create(type, AttributeMode.Strict);
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
            return _dictionary.TryGetValue(request.table, out var source) ? source.Parse(request) : null;
        }
    }
}