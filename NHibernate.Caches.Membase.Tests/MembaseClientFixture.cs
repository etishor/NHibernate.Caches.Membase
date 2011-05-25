
using System.Collections.Generic;
using System.Threading;
using Nhibernate.Caches.Membase;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.Membase.Tests
{
    [TestFixture]
    public class MembaseClientFixture
    {
        private MembaseCacheProvider provider;
        private Dictionary<string, string> props;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            props = new Dictionary<string, string>();
            provider = new MembaseCacheProvider();
            provider.Start(props);
        }

        [TestFixtureTearDown]
        public void FixtureStop()
        {
            provider.Stop();
        }

        [Test]
        public void TestClear()
        {
            string key = "key1";
            string value = "value";

            ICache cache = provider.BuildCache("nunit", props);
            Assert.IsNotNull(cache, "no cache returned");

            // add the item
            cache.Put(key, value);
            Thread.Sleep(1000);

            // make sure it's there
            object item = cache.Get(key);
            Assert.IsNotNull(item, "couldn't find item in cache");

            // clear the cache
            cache.Clear();

            // make sure we don't get an item
            item = cache.Get(key);
            Assert.IsNull(item, "item still exists in cache");
        }

        [Test]
        public void TestDefaultConstructor()
        {
            ICache cache = new MembaseCache();
            Assert.IsNotNull(cache);
        }

        [Test]
        public void TestEmptyProperties()
        {
            ICache cache = new MembaseCache("nunit", new Dictionary<string, string>());
            Assert.IsNotNull(cache);
        }

        [Test]
        public void TestNoPropertiesConstructor()
        {
            ICache cache = new MembaseCache("nunit");
            Assert.IsNotNull(cache);
        }

        [Test]
        public void TestNullKeyGet()
        {
            ICache cache = new MembaseCache();
            cache.Put("nunit", "value");
            Thread.Sleep(1000);
            object item = cache.Get(null);
            Assert.IsNull(item);
        }

        [Test]
        public void TestNullKeyPut()
        {
            ICache cache = new MembaseCache();
            Assert.DoesNotThrow(() => cache.Put(null, null));
        }

        [Test]
        public void TestNullKeyRemove()
        {
            ICache cache = new MembaseCache();
            Assert.DoesNotThrow(() => cache.Remove(null));
        }

        [Test]
        public void TestNullValuePut()
        {
            ICache cache = new MembaseCache();
            Assert.DoesNotThrow(() => cache.Put("nunit",null));
        }

        [Test]
        public void TestPut()
        {
            string key = "key1";
            string value = "value";

            ICache cache = provider.BuildCache("nunit", props);
            Assert.IsNotNull(cache, "no cache returned");

            Assert.IsNull(cache.Get(key), "cache returned an item we didn't add !?!");

            cache.Put(key, value);
            Thread.Sleep(1000);
            object item = cache.Get(key);
            Assert.IsNotNull(item);
            Assert.AreEqual(value, item, "didn't return the item we added");
        }

        [Test]
        public void TestRegions()
        {
            string key = "key";
            ICache cache1 = provider.BuildCache("nunit1", props);
            ICache cache2 = provider.BuildCache("nunit2", props);
            string s1 = "test1";
            string s2 = "test2";
            cache1.Put(key, s1);
            cache2.Put(key, s2);
            Thread.Sleep(1000);
            object get1 = cache1.Get(key);
            object get2 = cache2.Get(key);
            Assert.IsFalse(get1 == get2);
        }

        [Test]
        public void TestRemove()
        {
            string key = "key1";
            string value = "value";

            ICache cache = provider.BuildCache("nunit", props);
            Assert.IsNotNull(cache, "no cache returned");

            // add the item
            cache.Put(key, value);
            Thread.Sleep(1000);

            // make sure it's there
            object item = cache.Get(key);
            Assert.IsNotNull(item, "item just added is not there");

            // remove it
            cache.Remove(key);

            // make sure it's not there
            item = cache.Get(key);
            Assert.IsNull(item, "item still exists in cache");
        }
    }
}