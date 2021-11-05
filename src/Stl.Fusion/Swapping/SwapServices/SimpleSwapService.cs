using Microsoft.Extensions.DependencyInjection;
using Stl.OS;

namespace Stl.Fusion.Swapping;

public class SimpleSwapService : SwapServiceBase
{
    public class Options
    {
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan TimerQuanta { get; set; } = TimeSpan.FromSeconds(1);
        public int ConcurrencyLevel { get; set; } = HardwareInfo.GetProcessorCountPo2Factor();
        public Func<IUtf16Serializer<object>> SerializerFactory { get; set; } =
            () => new NewtonsoftJsonSerializer().ToTyped<object>();
        public IMomentClock? Clock { get; set; }
    }

    protected readonly ConcurrentDictionary<string, string> Storage;
    protected readonly ConcurrentTimerSet<string> ExpirationTimers;
    public TimeSpan ExpirationTime { get; }
    public IMomentClock Clock { get; }

    public SimpleSwapService(Options? options, IServiceProvider services)
    {
        options ??= new();
        SerializerFactory = options.SerializerFactory;
        ExpirationTime = options.ExpirationTime;
        Clock = options.Clock ?? services.GetRequiredService<MomentClockSet>().CoarseCpuClock;
        Storage = new ConcurrentDictionary<string, string>(
            options.ConcurrencyLevel,
            ComputedRegistry.Options.DefaultInitialCapacity,
            StringComparer.Ordinal);
        ExpirationTimers = new ConcurrentTimerSet<string>(
            new ConcurrentTimerSet<string>.Options() {
                Clock = Clock,
                Quanta = options.TimerQuanta,
                ConcurrencyLevel = options.ConcurrencyLevel,
            },
            key => Storage.TryRemove(key, out _));
    }

    protected override ValueTask<string?> Load(string key, CancellationToken cancellationToken)
    {
        if (!Storage.TryGetValue(key, out var value))
            return ValueTaskExt.FromResult((string?) null);
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
        return ValueTaskExt.FromResult(value)!;
    }

    protected override ValueTask<bool> Renew(string key, CancellationToken cancellationToken)
    {
        if (!Storage.TryGetValue(key, out var value))
            return ValueTaskExt.FalseTask;
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
        return ValueTaskExt.TrueTask;
    }

    protected override ValueTask Store(string key, string value, CancellationToken cancellationToken)
    {
        Storage[key] = value;
        ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
        return ValueTaskExt.CompletedTask;
    }
}
