using NUnit.Framework;
using Searchlight.Caching;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Searchlight.Tests.Caching
{
    public class CacheTests
    {
        private class SimpleCacheTest : ObjectCache<string>
        {
            public SimpleCacheTest()
            {
                this._cacheDuration = new TimeSpan(0, 0, 1);
            }

            public int ReloadCount { get; set; }

            protected override string ReloadCache()
            {
                ReloadCount++;
                return "TEST";
            }
        }

        [Test(Description ="Cache.TriggerReload")]
        public void CacheTriggerReload()
        {
            // Verify object cache works
            SimpleCacheTest sct = new SimpleCacheTest();
            Assert.AreEqual("TEST", sct.Get());
            Assert.True(sct.ReloadCount >= 1);

            // Force a full reload
            CacheHelper.ResetAllCaches();

            // Run a loop for 10 seconds
            DateTime start = DateTime.UtcNow;
            int secElapsed = 0;
            while (true) {

                // Test the value once every 100 msec
                Task.Delay(100);
                Assert.AreEqual("TEST", sct.Get());

                // Report results every 1 sec
                var ts = DateTime.UtcNow - start;
                if ((int)ts.TotalSeconds > secElapsed) {
                    System.Diagnostics.Debug.WriteLine($"Test Results: {ts.TotalSeconds} sec elapsed, {sct.ReloadCount} reload count");
                    secElapsed = (int)ts.TotalSeconds;

                    // Break the test after 10sec
                    if (secElapsed == 10) break;
                }
            }

            // We should have at least 9 reloads over 10 seconds
            Assert.True(sct.ReloadCount >= 9);
        }

        [Test(Description = "Cache.Basics")]
        public void CacheBasics()
        {
            // Construct a bad cache system and expect it to return null
            var oc = new ObjectCache<string>();
            Assert.Throws<NotImplementedException>(() => { oc.Get(); });
        }

        private class DictionaryCacheTest : DictionaryCache<string, string>
        {
            public DictionaryCacheTest()
            {
                this._cacheDuration = new TimeSpan(0, 0, 1);
            }

            public int ReloadCount { get; set; }

            protected override Dictionary<string, string> ReloadCache()
            {
                ReloadCount++;
                var dict = new Dictionary<string, string>();
                dict["test"] = "TEST";
                return dict;
            }
        }
        [Test(Description = "Cache.Dictionary")]
        public void CacheDictionary()
        {
            // Construct a bad cache system and expect it to return null
            var dict = new DictionaryCacheTest();
            Assert.AreEqual("TEST", dict.GetItem("test"));
            Assert.Null(dict.GetItem("somethingelse"));
        }
    }
}
