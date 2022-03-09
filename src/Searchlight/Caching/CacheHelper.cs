using System;

namespace Searchlight.Caching
{
    /// <summary>
    /// Searchlight originally was written to query objects from an in-memory cache.  These cache classes
    /// are still available if you need them.
    /// </summary>
    public class CacheHelper
    {
        /// <summary>
        /// Hook this to determine when all caches are reset
        /// </summary>
        public static event EventHandler ResetAllCachesEvent;

        /// <summary>
        /// Reset all caches
        /// </summary>
        public static void ResetAllCaches()
        {
            ResetAllCachesEvent?.Invoke(null, EventArgs.Empty);
        }
    }
}
