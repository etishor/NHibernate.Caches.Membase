//-----------------------------------------------------------------------
// <copyright file="MembaseCacheProvider.cs" company="Iulian Margarintescu">
// This file is released under the NHibernate license 
// Iulian Margarintescu eti@erata.net
// http://www.erata.net
// </copyright>
//-----------------------------------------------------------------------

namespace Nhibernate.Caches.Membase
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using global::Membase;
    using global::Membase.Configuration;
    using NHibernate;
    using NHibernate.Cache;

    /// <summary>
    /// NHibernate Cache provider for Membase.
    /// See: http://www.membase.org/
    /// </summary>
    public sealed class MembaseCacheProvider : ICacheProvider
    {
        /// <summary>
        /// logger instance for this class
        /// </summary>
        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(MembaseCacheProvider));

        /// <summary>
        /// Configuration section for this provider
        /// </summary>
        private static readonly IMembaseClientConfiguration config = ReadConfig();

        /// <summary>
        /// Guard for thread safe static operations
        /// </summary>
        private static readonly object syncObject = new object();

        /// <summary>
        /// The shared client instance. Get/Store operations with the client are thread safe.
        /// </summary>
        private static MembaseClient clientInstance = null;

        /// <summary>
        /// Configure the cache
        /// </summary>
        /// <param name="regionName">the name of the cache region</param>
        /// <param name="properties">configuration settings</param>
        /// <returns>The instance of the cache</returns>
        public ICache BuildCache(string regionName, IDictionary<string, string> properties)
        {
            if (regionName == null)
            {
                regionName = string.Empty;
            }

            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }

            if (log.IsDebugEnabled)
            {
                var sb = new StringBuilder();
                foreach (var pair in properties)
                {
                    sb.Append("name=");
                    sb.Append(pair.Key);
                    sb.Append("&value=");
                    sb.Append(pair.Value);
                    sb.Append(";");
                }

                log.Debug("building cache with region: " + regionName + ", properties: " + sb);
            }

            return new MembaseCache(regionName, properties, clientInstance);
        }

        /// <summary>
        /// generate a timestamp
        /// </summary>
        /// <returns>The new timestamp</returns>
        public long NextTimestamp()
        {
            return Timestamper.Next();
        }

        /// <summary>
        /// Callback to perform any necessary initialization of the underlying cache implementation
        /// during ISessionFactory construction.
        /// </summary>
        /// <param name="properties">current configuration settings</param>
        public void Start(IDictionary<string, string> properties)
        {            
            lock (syncObject)
            {
                if (config == null)
                {
                    throw new ConfigurationErrorsException("Configuration for membase not found");
                }

                if (clientInstance == null)
                {
                    clientInstance = new MembaseClient(config);
                }
            }
        }

        /// <summary>
        /// Callback to perform any necessary cleanup of the underlying cache implementation
        /// during <see cref="M:NHibernate.ISessionFactory.Close"/>.
        /// </summary>
        public void Stop()
        {
            lock (syncObject)
            {
                clientInstance.Dispose();
                clientInstance = null;
            }
        }

        /// <summary>
        /// Reads the config for the membase client.
        /// </summary>
        /// <returns>The config section</returns>
        private static IMembaseClientConfiguration ReadConfig()
        {
            var config = ConfigurationManager.GetSection("membase") as IMembaseClientConfiguration;
            if (config != null)
            {
                return config;
            }

            if (log.IsInfoEnabled)
            {
                log.Info("membase configuration section not found, using default pool http://localhost:8091/pools/default.");
            }

            config = new MembaseClientConfiguration();
            config.Urls.Add(new Uri("http://localhost:8091/pools/default"));
            return config;
        }
    }
}
