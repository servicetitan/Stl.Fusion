using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Interception;
using Stl.OS;
using Stl.Time;

namespace Stl.Fusion.Caching
{
    public class SimpleCache : ICache
    {
        public class Options
        {
            public TimeSpan MaxExpirationTime { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan Quanta { get; set; } = TimeSpan.FromSeconds(1);
            public int ConcurrencyLevel { get; set; } = HardwareInfo.ProcessorCount;
            public IMomentClock? Clock { get; set; }
        }

        protected static readonly Option<Result<object>> None = Option<Result<object>>.None;
        protected readonly ConcurrentDictionary<InterceptedInput, (Result<object> Value, TimeSpan ExpirationTime)> Storage;
        protected readonly ConcurrentTimerSet<InterceptedInput> ExpirationTimers;
        public TimeSpan MaxExpirationTime { get; }
        public IMomentClock Clock { get; }

        public SimpleCache(
            Options? options = null,
            IMomentClock? clock = null)
        {
            options ??= new Options();
            MaxExpirationTime = options.MaxExpirationTime;
            Clock = clock ?? options.Clock ?? CoarseCpuClock.Instance;
            Storage = new ConcurrentDictionary<InterceptedInput,(Result<object>, TimeSpan)>(
                options.ConcurrencyLevel,
                ComputedRegistry.Options.DefaultInitialCapacity);
            ExpirationTimers = new ConcurrentTimerSet<InterceptedInput>(new ConcurrentTimerSet<InterceptedInput>.Options() {
                Clock = Clock,
                Quanta = options.Quanta,
                ConcurrencyLevel = options.ConcurrencyLevel,
                FireHandler = key => Storage.TryRemove(key, out _)
            });
        }

        public ValueTask SetAsync(InterceptedInput key, Result<object> value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            if (expirationTime > MaxExpirationTime)
                expirationTime = MaxExpirationTime;
            Storage[key] = (value, expirationTime);
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + expirationTime);
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public ValueTask RemoveAsync(InterceptedInput key, CancellationToken cancellationToken)
        {
            Storage.Remove(key, out _);
            ExpirationTimers.Remove(key);
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public virtual ValueTask<Option<Result<object>>> GetAsync(InterceptedInput key, CancellationToken cancellationToken)
        {
            if (!Storage.TryGetValue(key, out var pair))
                return ValueTaskEx.FromResult(None);
            var (value, expirationTime) = pair;
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + expirationTime);
            return ValueTaskEx.FromResult(Option.Some(value));
        }
    }
}
