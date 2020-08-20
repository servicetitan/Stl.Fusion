using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.OS;
using Stl.Time;

namespace Stl.Fusion.Caching
{
    public class SimpleCache<TKey> : ICache<TKey>
        where TKey : notnull
    {
        public class Options
        {
            public TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan MaxExpirationTime { get; set; } = TimeSpan.FromMinutes(1);
            public TimeSpan TimerQuanta { get; set; } = TimeSpan.FromSeconds(1);
            public int ConcurrencyLevel { get; set; } = HardwareInfo.ProcessorCount;
            public IMomentClock? Clock { get; set; }
        }

        protected readonly ConcurrentDictionary<TKey, (object Value, TimeSpan ExpirationTime)> Storage;
        protected readonly ConcurrentTimerSet<TKey> ExpirationTimers;
        public TimeSpan DefaultExpirationTime { get; }
        public TimeSpan MaxExpirationTime { get; }
        public IMomentClock Clock { get; }

        public SimpleCache(
            Options? options = null,
            IMomentClock? clock = null)
        {
            options ??= new Options();
            DefaultExpirationTime = options.DefaultExpirationTime;
            MaxExpirationTime = options.MaxExpirationTime;
            Clock = clock ?? options.Clock ?? CoarseCpuClock.Instance;
            Storage = new ConcurrentDictionary<TKey, (object Value, TimeSpan ExpirationTime)>(
                options.ConcurrencyLevel,
                ComputedRegistry.Options.DefaultInitialCapacity);
            ExpirationTimers = new ConcurrentTimerSet<TKey>(new ConcurrentTimerSet<TKey>.Options() {
                Clock = Clock,
                Quanta = options.TimerQuanta,
                ConcurrencyLevel = options.ConcurrencyLevel,
                FireHandler = key => Storage.TryRemove(key, out _)
            });
        }

        public ValueTask SetAsync(TKey key, object value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            if (expirationTime > MaxExpirationTime)
                expirationTime = MaxExpirationTime;
            else if (expirationTime == TimeSpan.Zero)
                expirationTime = DefaultExpirationTime;
            Storage[key] = (value, expirationTime);
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + expirationTime);
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken)
        {
            Storage.Remove(key, out _);
            ExpirationTimers.Remove(key);
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public virtual ValueTask<Option<object>> GetAsync(TKey key, CancellationToken cancellationToken)
        {
            if (!Storage.TryGetValue(key, out var pair))
                return ValueTaskEx.FromResult(Option<object>.None);
            var (value, expirationTime) = pair;
            ExpirationTimers.AddOrUpdateToLater(key, Clock.Now + expirationTime);
            return ValueTaskEx.FromResult(Option.Some(value));
        }
    }
}
