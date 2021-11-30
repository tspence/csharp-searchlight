using System;

namespace Searchlight.Caching
{
    public class CacheHelper
    {
        #region Static Cache Reset Event
        // Reset Cache Event
        public static event EventHandler ResetAllCachesEvent;

        /// <summary>
        /// Reset all caches
        /// </summary>
        public static void ResetAllCaches()
        {
            ResetAllCachesEvent?.Invoke(null, EventArgs.Empty);
        }
        #endregion
    }
}
