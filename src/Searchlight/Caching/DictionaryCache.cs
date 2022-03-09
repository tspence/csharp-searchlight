using System.Collections.Generic;

namespace Searchlight.Caching
{
    /// <summary>
    /// Represents a cache using a dictionary with defined keys.  This is similar to an IDictionary, but
    /// it can potentially trigger a cache-fetch.
    /// </summary>
    /// <typeparam name="KEY"></typeparam>
    /// <typeparam name="ITEM"></typeparam>
    public class DictionaryCache<KEY, ITEM> : ObjectCache<Dictionary<KEY, ITEM>>
    {
        /// <summary>
        /// Retrieve a single item from this cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ITEM GetItem(KEY id)
        {
            var dict = Get();
            if (dict.TryGetValue(id, out ITEM obj))
            {
                return obj;
            }
            return default;
        }

        /// <summary>
        /// Retrieve a list of all items stored in this cache
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITEM> GetAll()
        {
            var dict = Get();
            return dict.Values;
        }
    }
}
