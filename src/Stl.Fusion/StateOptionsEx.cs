using System;

namespace Stl.Fusion
{
    public static class StateOptionsEx
    {
        public static TOptions WithZeroUpdateDelay<TOptions>(this TOptions options)
            where TOptions : class, ILiveState.IOptions
            => options.WithUpdateDelayer(UpdateDelayer.None);

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

        public static TOptions WithUpdateDelayer<TOptions>(this TOptions options, TimeSpan delay)
            where TOptions : class, ILiveState.IOptions
            => options.WithUpdateDelayer(new UpdateDelayer.Options() { Delay = delay });

        public static TOptions WithUpdateDelayer<TOptions>(this TOptions options, double delayInSeconds)
            where TOptions : class, ILiveState.IOptions
            => options.WithUpdateDelayer(TimeSpan.FromSeconds(delayInSeconds));
    }
}
