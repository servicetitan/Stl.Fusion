using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    public class LoggingCacheWrapper<TCache> : ICache
        where TCache : ICache
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

        public ValueTask SetAsync(InterceptedInput key, Result<object> value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            if (IsEnabled)
                Log.Log(LogLevel, $"[=] {key} <- {value}");
            return Cache.SetAsync(key, value, expirationTime, cancellationToken);
        }

        public ValueTask RemoveAsync(InterceptedInput key, CancellationToken cancellationToken)
        {
            if (IsEnabled)
                Log.Log(LogLevel, $"[-] {key}");
            return Cache.RemoveAsync(key, cancellationToken);
        }

        public async ValueTask<Option<Result<object>>> GetAsync(InterceptedInput key, CancellationToken cancellationToken)
        {
            var value = await Cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (IsEnabled)
                Log.Log(LogLevel, $"[?] {key} -> {value}");
            return value;
        }
    }
}
