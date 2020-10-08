using System;

namespace Stl.Fusion
{
    public static class StateOptionsEx
    {
        public static TOptions WithInstantUpdates<TOptions>(this TOptions options)
            where TOptions : class, ILiveState.IOptions
            => options.WithUpdateDelayer(UpdateDelayer.Options.InstantUpdates);

        public static TOptions WithUpdateDelayer<TOptions>(this TOptions options, IUpdateDelayer updateDelayer)
            where TOptions : class, ILiveState.IOptions
        {
            options.UpdateDelayerFactory = _ => updateDelayer;
            return options;
        }

        public static TOptions WithUpdateDelayer<TOptions>(this TOptions options, UpdateDelayer.Options updateDelayerOptions)
            where TOptions : class, ILiveState.IOptions
        {
            options.UpdateDelayerFactory = _ => new UpdateDelayer(updateDelayerOptions);
            return options;
        }

        public static TOptions WithUpdateDelayer<TOptions>(this TOptions options, Action<UpdateDelayer.Options>? optionsBuilder)
            where TOptions : class, ILiveState.IOptions
        {
            var updateDelayerOptions = new UpdateDelayer.Options();
            optionsBuilder?.Invoke(updateDelayerOptions);
            return options.WithUpdateDelayer(updateDelayerOptions);
        }

        public static TOptions WithUpdateDelayer<TOptions>(
            this TOptions options,
            TimeSpan delay, TimeSpan? maxExtraErrorDelay = null)
            where TOptions : class, ILiveState.IOptions
        {
            var o = new UpdateDelayer.Options() { Delay = delay };
            if (maxExtraErrorDelay.HasValue)
                o.MaxExtraErrorDelay = maxExtraErrorDelay.GetValueOrDefault();
            return options.WithUpdateDelayer(o);
        }

        public static TOptions WithUpdateDelayer<TOptions>(
            this TOptions options,
            double delayInSeconds, double? maxExtraErrorDelayInSeconds = null)
            where TOptions : class, ILiveState.IOptions
        {
            var o = new UpdateDelayer.Options() { Delay = TimeSpan.FromSeconds(delayInSeconds) };
            if (maxExtraErrorDelayInSeconds.HasValue)
                o.MaxExtraErrorDelay = TimeSpan.FromSeconds(maxExtraErrorDelayInSeconds.GetValueOrDefault());
            return options.WithUpdateDelayer(o);
        }
    }
}
