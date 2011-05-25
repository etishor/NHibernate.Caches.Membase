using System.Collections.Generic;
using Nhibernate.Caches.Membase;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.Membase.Tests
{
    [TestFixture]
    public class MembaseProviderFixture
    {
        private ICacheProvider provider;
        private Dictionary<string, string> props;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            props = new Dictionary<string, string>();
            provider = new MembaseCacheProvider();
            provider.Start(props);
        }

        [TestFixtureTearDown]
        public void Stop()
        {
            provider.Stop();
        }

        [Test]
        public void TestBuildCacheFromConfig()
        {
            ICache cache = provider.BuildCache("foo", null);
            Assert.IsNotNull(cache, "pre-configured cache not found");
        }

        [Test]
        public void TestBuildCacheNullNull()
        {
            ICache cache = provider.BuildCache(null, null);
            Assert.IsNotNull(cache, "no cache returned");
        }

        [Test]
        public void TestBuildCacheStringICollection()
        {
            ICache cache = provider.BuildCache("another_region", props);
            Assert.IsNotNull(cache, "no cache returned");
        }

        [Test]
        public void TestBuildCacheStringNull()
        {
            ICache cache = provider.BuildCache("a_region", null);
            Assert.IsNotNull(cache, "no cache returned");
        }

        [Test]
        public void TestNextTimestamp()
        {
            long ts = provider.NextTimestamp();
            Assert.IsNotNull(ts, "no timestamp returned");
        }
    }
}