using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Searchlight.Caching
{
    /// <summary>
    /// Represents a cache of items where each item must be fetched individually based on the "KEY" provided
    /// </summary>
    /// <typeparam name="KEY">The key used to fetch the item</typeparam>
    /// <typeparam name="ITEM">The data type of the item object</typeparam>
    public class ItemFetchCache<KEY, ITEM>
    {
        /// <summary>
        /// Concurrent dictionary for fetching items
        /// </summary>
        private readonly ConcurrentDictionary<KEY, ItemCache<ITEM>> _item_dict = new();

        /// <summary>
        /// Default time frame is that items expire after an hour
        /// </summary>
        protected TimeSpan _cacheDuration = new(1, 0, 0);


        #region Constructor
        public ItemFetchCache()
        {
            // Set defaults
            _item_dict = new ConcurrentDictionary<KEY, ItemCache<ITEM>>();

            // Hook this to the overall cache reset event
            CacheHelper.ResetAllCachesEvent += this.ResetCacheHandler;
        }
        #endregion

        #region Reset cache
        /// <summary>
        /// Private helper to reset the cache based on a universal reset event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetCacheHandler(object sender, EventArgs e)
        {
            _item_dict.Clear();
        }
        #endregion

        #region Overrides
        /// <summary>
        /// You must override this function to fetch one item by its specified key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual bool TryFetchItem(KEY key, out ITEM item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// If your fetch cache should be pre-populated with some some presets, override this to preload the cache with certain items
        /// </summary>
        protected virtual void PreloadCache()
        {
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// Fetch an item by its specified key, and throw an exception if the item is not present.  A null return is a valid return from this function.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ITEM Get(KEY key)
        {
            bool success = TryGet(key, out ITEM item);
            if (!success)
            {
                throw new KeyNotFoundException();
            }
            return item;
        }

        /// <summary>
        /// Fetch an item by its specified key, and return true if successful.  This function can return a null object if one is pushed into the class, or if TryFetchItem failed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGet(KEY key, out ITEM item)
        {
            bool result = false;
            var found = _item_dict.TryGetValue(key, out ItemCache<ITEM> cached_obj);
            var maximum_cache_age = DateTime.UtcNow.Subtract(_cacheDuration);

            // Did we find an object successfully, and is it recent enough?
            if (found && cached_obj.CacheDate < maximum_cache_age)
            {
                item = cached_obj.CachedObject;
                result = true;
            }
            else
            {
                var success = TryFetchItem(key, out item);
                if (success)
                {
                    Set(key, item, DateTime.UtcNow);
                    result = true;
                }
            }

            // Here's what we've got - nulls are acceptable
            return result;
        }
        #endregion

        #region Internal Interface
        /// <summary>
        /// Call this function to put an object into the cache with a specified date/time 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="item_loaded_timestamp"></param>
        protected void Set(KEY key, ITEM item, DateTime item_loaded_timestamp)
        {
            ItemCache<ITEM> cache = new()
            {
                CachedObject = item,
                CacheDate = item_loaded_timestamp
            };
            _item_dict[key] = cache;
        }
        #endregion
    }
}
