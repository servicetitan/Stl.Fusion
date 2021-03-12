using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping
{
    public class LoggingSwapServiceWrapper<TSwapService> : ISwapService
        where TSwapService : ISwapService
    {
        public class Options
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
            options ??= new();
            loggerFactory ??= NullLoggerFactory.Instance;
            SwapService = swapService;
            Log = loggerFactory.CreateLogger(swapService.GetType());
            LogLevel = options.LogLevel;
            IsEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
        }

        public async ValueTask<IResult?> Load((ComputeMethodInput Input, LTag Version) key, CancellationToken cancellationToken = default)
        {
            var value = await SwapService.Load(key, cancellationToken).ConfigureAwait(false);
            if (IsEnabled)
                Log.Log(LogLevel, "[?] {Key} -> {Value}", key, value);
            return value;
        }

        public ValueTask Store((ComputeMethodInput Input, LTag Version) key, IResult value,
            CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
                Log.Log(LogLevel, "[=] {Key} <- {Value}", key, value);
            return SwapService.Store(key, value, cancellationToken);
        }
    }
}
