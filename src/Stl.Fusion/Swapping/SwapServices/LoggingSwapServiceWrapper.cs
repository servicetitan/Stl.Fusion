using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.DependencyInjection;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping
{
    public class LoggingSwapServiceWrapper<TSwapService> : ISwapService
        where TSwapService : ISwapService
    {
        public class Options : IOptions
        {
            public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        }

        protected readonly ILogger Log;
        protected readonly TSwapService SwapService;
        protected LogLevel LogLevel;
        protected bool IsEnabled;

        public LoggingSwapServiceWrapper(
            Options? options,
            TSwapService swapService,
            ILoggerFactory? loggerFactory = null)
        {
            options = options.OrDefault();
            loggerFactory ??= NullLoggerFactory.Instance;
            SwapService = swapService;
            Log = loggerFactory.CreateLogger(swapService.GetType());
            LogLevel = options.LogLevel;
            IsEnabled = Log.IsEnabled(LogLevel);
        }

        public async ValueTask<IResult?> LoadAsync((InterceptedInput Input, LTag Version) key, CancellationToken cancellationToken = default)
        {
            var value = await SwapService.LoadAsync(key, cancellationToken).ConfigureAwait(false);
            if (IsEnabled)
                Log.Log(LogLevel, $"[?] {key} -> {value}");
            return value;
        }

        public ValueTask StoreAsync((InterceptedInput Input, LTag Version) key, IResult value,
            CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
                Log.Log(LogLevel, $"[=] {key} <- {value}");
            return SwapService.StoreAsync(key, value, cancellationToken);
        }
    }
}
