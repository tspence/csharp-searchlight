using System;

namespace Searchlight.Caching
{
    /// <summary>
    /// Keeps track of a singleton and regenerates it on demand based on cache rules
    /// </summary>
    /// <typeparam name="ITEM"></typeparam>
    public class ItemCache<ITEM>
    {
        /// <summary>
        /// This is the object cached
        /// </summary>
        public ITEM CachedObject { get; set; }

        /// <summary>
        /// This is the date/time when this object was cached; use this date to determine if the item is stale
        /// </summary>
        public DateTime CacheDate { get; set; }
    }
}
