//-----------------------------------------------------------------------
// <copyright file="MembaseCache.cs" company="Iulian Margarintescu">
// This file is released under the NHibernate license 
// Iulian Margarintescu eti@erata.net
// http://www.erata.net
// </copyright>
//-----------------------------------------------------------------------

namespace Nhibernate.Caches.Membase
{
    using System;
    using System.Collections.Generic;
    using global::Membase;
    using NHibernate;
    using NHibernate.Cache;
    using Environment = NHibernate.Cfg.Environment;

    /// <summary>
    /// NHibernate Cache implementation for Membase
    /// </summary>
    public sealed class MembaseCache : ICache, IDisposable
    {
        /// <summary>
        /// Logger for this class
        /// </summary>
        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(MembaseCache));

        /// <summary>
        /// The Membase client
        /// </summary>
        private readonly MembaseClient client;

        /// <summary>
        /// Expiration timespan in seconds.
        /// </summary>
        private readonly int expiry;

        /// <summary>
        /// Region of the cache to use.
        /// </summary>
        private readonly string region;

        /// <summary>
        /// Region prefix for the cached items
        /// </summary>
        private readonly string regionPrefix = string.Empty;

        /// <summary>
        /// Flag indicating whether this instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembaseCache"/> class.
        /// </summary>
        public MembaseCache()
            : this("nhibernate", null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MembaseCache"/> class.
        /// </summary>
        /// <param name="regionName">Name of the region.</param>
        public MembaseCache(string regionName)
            : this(regionName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MembaseCache"/> class.
        /// </summary>
        /// <param name="regionName">Name of the region.</param>
        /// <param name="properties">The properties.</param>
        public MembaseCache(string regionName, IDictionary<string, string> properties)
            : this(regionName, properties, new MembaseClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MembaseCache"/> class.
        /// </summary>
        /// <param name="regionName">Name of the region.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="client">The client.</param>
        public MembaseCache(string regionName, IDictionary<string, string> properties, MembaseClient client)
        {
            this.region = regionName;
            this.client = client;
            this.expiry = 300;

            if (properties != null)
            {
                string expirationString = GetExpirationString(properties);
                if (expirationString != null)
                {
                    this.expiry = Convert.ToInt32(expirationString, System.Globalization.CultureInfo.InvariantCulture);
                    if (log.IsDebugEnabled)
                    {
                        log.DebugFormat("using expiration of {0} seconds", this.expiry);
                    }
                }

                if (properties.ContainsKey("regionPrefix"))
                {
                    this.regionPrefix = properties["regionPrefix"];
                    if (log.IsDebugEnabled)
                    {
                        log.DebugFormat("new regionPrefix :{0}", this.regionPrefix);
                    }
                }
                else
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("no regionPrefix value given, using defaults");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the cache region
        /// </summary>
        public string RegionName
        {
            get { return this.region; }
        }

        /// <summary>
        /// Gets a reasonable "lock timeout"
        /// </summary>
        public int Timeout
        {
            get { return Timestamper.OneMs * 60000; }
        }

        /// <summary>
        /// Clear the Cache
        /// </summary>
        public void Clear()
        {
            if (log.IsDebugEnabled)
            {
                log.Debug("Clearing cache.");
            }

            this.client.FlushAll();
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Destroy()
        {
            if (log.IsDebugEnabled)
            {
                log.Debug("Destroying cache.");
            }

            this.Clear();
        }

        /// <summary>
        /// Get the object from the Cache
        /// </summary>
        /// <param name="key">Key of the item in the cache</param>
        /// <returns>The item from the cache.</returns>
        public object Get(object key)
        {
            // no sense asking for item without a key
            if (key == null || string.IsNullOrEmpty(key.ToString()))
            {
                return null;
            }

            object result = this.client.Get(key.ToString());
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Getting object from cache for key {0} result {1}", key, result);
            }

            return result;
        }

        /// <summary>
        /// Generate a timestamp
        /// </summary>
        /// <returns>A new timestamp</returns>
        public long NextTimestamp()
        {
            return Timestamper.Next();
        }

        /// <summary>
        /// Puts a new object in the cache
        /// </summary>
        /// <param name="key">Key of the item to put in the cache</param>
        /// <param name="value">The item to put in the cache</param>
        public void Put(object key, object value)
        {
            // no sense putting an object without a key in the cache
            if (key == null || string.IsNullOrEmpty(key.ToString()))
            {
                return;
            }

            // no point in caching nulls.
            if (value == null)
            {
                return;
            }

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Putting object in cache with key {0} : {1}", key, value);
            }

            this.client.Store(Enyim.Caching.Memcached.StoreMode.Set, key.ToString(), value);
        }

        /// <summary>
        /// Remove an item from the Cache.
        /// </summary>
        /// <param name="key">The Key of the Item in the Cache to remove.</param>
        public void Remove(object key)
        {
            // no sense putting an object without a key in the cache
            if (key == null || string.IsNullOrEmpty(key.ToString()))
            {
                return;
            }

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Removing object from cache with key {0}", key);
            }

            this.client.Remove(key.ToString());
        }

        /// <summary>
        /// If this is a clustered cache, unlock the item
        /// </summary>
        /// <param name="key">The Key of the Item in the Cache to unlock.</param>
        /// <exception cref="T:NHibernate.Cache.CacheException">If the cache is unable to unlock the item.</exception>
        /// <remarks>
        /// This operation is not supported.
        /// </remarks>
        public void Unlock(object key)
        {
            if (log.IsWarnEnabled)
            {
                log.WarnFormat("Unlock request for key {0}. No locking implementation.", key);
            }
        }

        /// <summary>
        /// If this is a clustered cache, lock the item
        /// </summary>
        /// <param name="key">The Key of the Item in the Cache to lock.</param>
        /// <exception cref="T:NHibernate.Cache.CacheException">If the cache is unable to lock the item.</exception>
        /// <remarks>This operation is not supported.</remarks>
        public void Lock(object key)
        {
            if (log.IsWarnEnabled)
            {
                log.WarnFormat("Lock request for key {0}. No locking implementation.", key);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.client.Dispose();
            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the expiration string.
        /// </summary>
        /// <param name="props">The props.</param>
        /// <returns>The expiration value</returns>
        private static string GetExpirationString(IDictionary<string, string> props)
        {
            string result;
            if (!props.TryGetValue("expiration", out result))
            {
                props.TryGetValue(Environment.CacheDefaultExpiration, out result);
            }

            return result;
        }
    }
}
