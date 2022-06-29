using Stl.OS;

namespace Stl.Fusion.Swapping;

public class SimpleSwapService : SwapServiceBase
{
    public record Options
    {
        public TimeSpan ExpirationTime { get; init; } = TimeSpan.FromMinutes(1);
        public TimeSpan TimerQuanta { get; init; } = TimeSpan.FromSeconds(1);
        public int ConcurrencyLevel { get; init; } = HardwareInfo.GetProcessorCountPo2Factor();
        public Func<ITextSerializer<object>> SerializerFactory { get; init; } =
            () => new NewtonsoftJsonSerializer().ToTyped<object>();
        public IMomentClock? Clock { get; init; }
    }

    protected ConcurrentDictionary<string, string> Storage { get; }
    protected ConcurrentTimerSet<string> ExpirationTimers { get; }

    public Options Settings { get; }
    public IMomentClock Clock { get; }

    public SimpleSwapService(Options settings, IServiceProvider services)
    {
        Settings = settings;
        SerializerFactory = settings.SerializerFactory;
        Clock = settings.Clock ?? services.GetRequiredService<MomentClockSet>().CpuClock;
        Storage = new ConcurrentDictionary<string, string>(
            settings.ConcurrencyLevel,
            ComputedRegistry.Options.DefaultInitialCapacity,
            StringComparer.Ordinal);
        ExpirationTimers = new ConcurrentTimerSet<string>(
            new() {
                Quanta = settings.TimerQuanta,
                ConcurrencyLevel = settings.ConcurrencyLevel,
                Clock = Clock,
            },
            key => Storage.TryRemove(key, out _));
    }

    protected override ValueTask<string?> Load(string key, CancellationToken cancellationToken)
    {
        if (!Storage.TryGetValue(key, out var value))
            return ValueTaskExt.FromResult((string?) null);
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + Settings.ExpirationTime);
        return ValueTaskExt.FromResult(value)!;
    }

    protected override ValueTask<bool> Touch(string key, CancellationToken cancellationToken)
    {
        if (!Storage.TryGetValue(key, out var value))
            return ValueTaskExt.FalseTask;
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + Settings.ExpirationTime);
        return ValueTaskExt.TrueTask;
    }

    protected override ValueTask Store(string key, string value, CancellationToken cancellationToken)
    {
        Storage[key] = value;
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + Settings.ExpirationTime);
        return ValueTaskExt.CompletedTask;
    }
}
