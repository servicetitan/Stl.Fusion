using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.OS;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.Swapping
{
    public class SimpleSwapService : SwapServiceBase<string>
    {
        public class Options : IOptions
        {
            public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan TimerQuanta { get; set; } = TimeSpan.FromSeconds(1);
            public int ConcurrencyLevel { get; set; } = HardwareInfo.ProcessorCount;
            public Func<ISerializer<string>> SerializerFactory { get; set; } = () => new JsonNetSerializer();
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        protected readonly ConcurrentDictionary<string, string> Storage;
        protected readonly ConcurrentTimerSet<string> ExpirationTimers;
        public TimeSpan ExpirationTime { get; }
        public IMomentClock Clock { get; }

        public SimpleSwapService(Options? options = null)
        {
            options = options.OrDefault();
            SerializerFactory = options.SerializerFactory;
            ExpirationTime = options.ExpirationTime;
            Clock = options.Clock;
            Storage = new ConcurrentDictionary<string, string>(
                options.ConcurrencyLevel,
                ComputedRegistry.Options.DefaultInitialCapacity);
            ExpirationTimers = new ConcurrentTimerSet<string>(
                new ConcurrentTimerSet<string>.Options() {
                    Clock = Clock,
                    Quanta = options.TimerQuanta,
                    ConcurrencyLevel = options.ConcurrencyLevel,
                },
                key => Storage.TryRemove(key, out _));
        }

        protected override ValueTask<Option<string>> LoadAsync(string key, CancellationToken cancellationToken)
        {
            if (!Storage.TryGetValue(key, out var value))
                return ValueTaskEx.FromResult(Option.None<string>());
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
            return ValueTaskEx.FromResult(Option.Some(value));
        }

        protected override ValueTask<bool> RenewAsync(string key, CancellationToken cancellationToken)
        {
            if (!Storage.TryGetValue(key, out var value))
                return ValueTaskEx.FalseTask;
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
            return ValueTaskEx.TrueTask;
        }

        protected override ValueTask StoreAsync(string key, string value, CancellationToken cancellationToken)
        {
            Storage[key] = value;
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + ExpirationTime);
            return ValueTaskEx.CompletedTask;
        }
    }
}
