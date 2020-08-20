using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Stl.Fusion.Caching
{
    public class LoggingCacheWrapper<TKey, TValue, TCache> : ICache<TKey, TValue>
        where TKey : notnull
        where TCache : ICache<TKey, TValue>
    {
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        }

        protected readonly ILogger Log;
        protected readonly TCache Cache;
        protected LogLevel LogLevel;
        protected bool IsEnabled;

        public LoggingCacheWrapper(
            TCache cache,
            Options? options = null,
            ILoggerFactory? loggerFactory = null)
        {
            options ??= new Options();
            Cache = cache;
            Log = loggerFactory.CreateLogger(cache.GetType());
            LogLevel = options.LogLevel;
            IsEnabled = Log.IsEnabled(LogLevel);
        }

        public ValueTask SetAsync(TKey key, TValue value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            if (IsEnabled)
                Log.Log(LogLevel, $"[=] {key} <- {value}");
            return Cache.SetAsync(key, value, expirationTime, cancellationToken);
        }

        public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken)
        {
            if (IsEnabled)
                Log.Log(LogLevel, $"[-] {key}");
            return Cache.RemoveAsync(key, cancellationToken);
        }

        public async ValueTask<Option<TValue>> GetAsync(TKey key, CancellationToken cancellationToken)
        {
            var value = await Cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (IsEnabled)
                Log.Log(LogLevel, $"[?] {key} -> {value}");
            return value;
        }
    }
}
