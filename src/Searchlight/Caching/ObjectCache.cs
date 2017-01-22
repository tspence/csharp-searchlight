using System;

namespace Searchlight.Caching
{
    public class ObjectCache<ITEM> 
    {
        private ITEM _item;
        private DateTime _next_cache_time;
        private readonly object _cache_lock = new object();
        private int _num_times_fetched = 0;

        /// <summary>
        /// Length of time to keep this cache before triggering a re-fetch
        /// </summary>
        protected TimeSpan _cacheDuration = new TimeSpan(2, 0, 0);

        #region Constructor
        public ObjectCache()
        {
            // Set defaults
            _item = default(ITEM);
            _next_cache_time = DateTime.MinValue;

            // Hook this to the overall cache reset event
            CacheHelper.ResetAllCachesEvent += this.ResetCacheHandler;
        }
        #endregion

        #region Internal functionality
        /// <summary>
        /// Test the cache and ensure that data is cached
        /// </summary>
        public void EnsureCache()
        {
            if (_item == null || _next_cache_time < DateTime.UtcNow) {
                lock (_cache_lock) {
                    if (_item == null || _next_cache_time < DateTime.UtcNow) {

                        // To avoid having multiple calls detect cache aging, next cache time is reset immediately; 
                        // We won't trigger again on age until RetrieveCacheSet finishes
                        _next_cache_time = DateTime.MaxValue;

                        // Two different paths - if the cache is empty, force it and do not return until the cache is loaded
                        if (_item == null) {
                            RetrieveCacheSet();

                        // Data exists, but it is stale.  Let's allow this function to return but begin the process of loading new data from the cache
                        } else {
                            System.Threading.Tasks.Task.Factory.StartNew(() => RetrieveCacheSet());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Wrapper to safely retrieve the data from the cache source and update our timings
        /// </summary>
        private void RetrieveCacheSet()
        {
            try {
                DateTime dt = DateTime.UtcNow;

                // Reassign the object rather than modifying the previous cached list.
                // This ensures that callers who have a reference to the previous object won't break when they are iterating over it.
                _item = ReloadCache();

                // Record how long we took
                //Log.Information("Cached {0} in {1}", this.GetType().Name, DateTime.UtcNow - dt);

                // Track how many times we've hit this object
                _num_times_fetched++;

            // Ensure that if the re-fetch statement crashes we recognize what happened
            //} catch (Exception ex) {
                //Log.Error("Exception while caching {1}: {2}", this.GetType().Name, ex.ToString());
            } finally {
                _next_cache_time = DateTime.UtcNow.Add(this._cacheDuration);
            }
        }

        /// <summary>
        /// Event handler that can be used to hook this to the overall cache reset event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ResetCacheHandler(object sender, EventArgs e)
        {
            this.ResetCache();
        }
        #endregion

        #region Functions to override
        /// <summary>
        /// Internal implementation of the function to load the cache from the source
        /// </summary>
        protected virtual ITEM ReloadCache()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Public Interfaces
        /// <summary>
        /// Flush this cache item and reload on the next call
        /// </summary>
        public void ResetCache()
        {
            // Ensure that no other object is in the midst of working on this while we reset it
            lock (_cache_lock) {
                _item = default(ITEM);
            }
        }

        /// <summary>
        /// Retrieve the object that is cached
        /// </summary>
        /// <returns></returns>
        public ITEM Get()
        {
            EnsureCache();
            return _item;
        }
        #endregion
    }
}
