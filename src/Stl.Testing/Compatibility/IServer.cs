#if NETFRAMEWORK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Testing
{
    /// <summary>Represents a server.</summary>
    public interface IServer : IDisposable
    {
        /// <summary>A collection of HTTP features of the server.</summary>
        IFeatureCollection Features { get; }

        /// <summary>Start the server with an application.</summary>
        /// <param name="application">An instance of <see cref="T:Microsoft.AspNetCore.Hosting.Server.IHttpApplication`1" />.</param>
        /// <typeparam name="TContext">The context associated with the application.</typeparam>
        /// <param name="cancellationToken">Indicates if the server startup should be aborted.</param>
        Task StartAsync<TContext>(
        IHttpApplication<TContext> application,
        CancellationToken cancellationToken);

        /// <summary>
        /// Stop processing requests and shut down the server, gracefully if possible.
        /// </summary>
        /// <param name="cancellationToken">Indicates if the graceful shutdown should be aborted.</param>
        Task StopAsync(CancellationToken cancellationToken);
    }

    public interface IFeatureCollection : IEnumerable<KeyValuePair<Type, object>>, IEnumerable
    {
        bool IsReadOnly { get; }

        int Revision { get; }

        object? this[Type key] { get; set; }

        TFeature Get<TFeature>();

        void Set<TFeature>(TFeature instance);
    }

       /// <summary>
    /// Default implementation for <see cref="IFeatureCollection"/>.
    /// </summary>
    public class FeatureCollection : IFeatureCollection
    {
        private static readonly KeyComparer FeatureKeyComparer = new KeyComparer();
        private readonly IFeatureCollection? _defaults;
        private readonly int _initialCapacity;
        private IDictionary<Type, object>? _features;
        private volatile int _containerRevision;

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureCollection"/>.
        /// </summary>
        public FeatureCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial number of elements that the collection can contain.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="initialCapacity"/> is less than 0</exception>
        public FeatureCollection(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            _initialCapacity = initialCapacity;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified defaults.
        /// </summary>
        /// <param name="defaults">The feature defaults.</param>
        public FeatureCollection(IFeatureCollection defaults)
        {
            _defaults = defaults;
        }

        /// <inheritdoc />
        public virtual int Revision
        {
            get { return _containerRevision + (_defaults?.Revision ?? 0); }
        }

        /// <inheritdoc />
        public bool IsReadOnly { get { return false; } }

        /// <inheritdoc />
        public object? this[Type key]
        {
            get {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                return _features != null && _features.TryGetValue(key, out var result) ? result : _defaults?[key];
            }
            set {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (value == null) {
                    if (_features != null && _features.Remove(key))
                        _containerRevision++;
                    return;
                }

                if (_features == null)
                    _features = new Dictionary<Type, object>(_initialCapacity);
                _features[key] = value;
                _containerRevision++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            if (_features != null) {
                foreach (var pair in _features)
                    yield return pair;
            }

            if (_defaults != null) {
                // Don't return features masked by the wrapper.
                foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, FeatureKeyComparer))
                    yield return pair;
            }
        }

        /// <inheritdoc />
        public TFeature? Get<TFeature>()
        {
            return (TFeature?)this[typeof(TFeature)];
        }

        /// <inheritdoc />
        public void Set<TFeature>(TFeature? instance)
        {
            this[typeof(TFeature)] = instance;
        }

        private class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
        {
            public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<Type, object> obj)
            {
                return obj.Key.GetHashCode();
            }
        }
    }

    public interface IServerAddressesFeature
    {
        ICollection<string> Addresses { get; }

        bool PreferHostingUrls { get; set; }
    }

    /// <summary>
    /// Specifies the address used by the server.
    /// </summary>
    public class ServerAddressesFeature : IServerAddressesFeature
    {
        /// <inheritdoc />
        public ICollection<string> Addresses { get; } = new List<string>();

        /// <inheritdoc />
        public bool PreferHostingUrls { get; set; }
    }

    /// <summary>Represents an application.</summary>
    /// <typeparam name="TContext">The context associated with the application.</typeparam>
    public interface IHttpApplication<TContext>
    {
        /// <summary>
        /// Create a TContext given a collection of HTTP features.
        /// </summary>
        /// <param name="contextFeatures">A collection of HTTP features to be used for creating the TContext.</param>
        /// <returns>The created TContext.</returns>
        TContext CreateContext(IFeatureCollection contextFeatures);

        /// <summary>Dispose a given TContext.</summary>
        /// <param name="context">The TContext to be disposed.</param>
        /// <param name="exception">The Exception thrown when processing did not complete successfully, otherwise null.</param>
        void DisposeContext(TContext context, Exception exception);

        /// <summary>Asynchronously processes an TContext.</summary>
        /// <param name="context">The TContext that the operation will process.</param>
        Task ProcessRequestAsync(TContext context);
    }

    internal class HostingApplication : IHttpApplication<HostingApplication.Context>
    {
        internal class Context
        {
        }

        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            return new Context();
        }

        public void DisposeContext(Context context, Exception exception)
        {
        }

        public Task ProcessRequestAsync(Context context)
        {
            return Task.CompletedTask;
        }
    }
}

#endif
