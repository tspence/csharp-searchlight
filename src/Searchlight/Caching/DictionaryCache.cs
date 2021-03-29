using System.Collections.Generic;

namespace Searchlight.Caching
{
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
            ITEM obj = default(ITEM);
            if (dict.TryGetValue(id, out obj))
            {
                return obj;
            }
            return default(ITEM);
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
