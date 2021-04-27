using System;
using System.Collections.Generic;

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
    }
}