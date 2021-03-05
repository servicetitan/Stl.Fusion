using System;
using System.Threading.Tasks;
using Pluralize.NET;
using Stl.Time;

namespace Stl.Fusion.Extensions.Internal
{
    public class LiveClock : ILiveClock
    {
        public class Options
        {
            public TimeSpan DefaultUpdatePeriod { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MaxInvalidationDelay { get; set; } = TimeSpan.FromMinutes(10);
            public IPluralize? Pluralize { get; set; }
            public IMomentClock? Clock { get; set; }
        }

        protected TimeSpan DefaultUpdatePeriod { get; set; }
        protected TimeSpan MaxInvalidationDelay { get; set; }
        protected IPluralize Pluralize { get; set; }
        protected IMomentClock Clock { get; set; }

        public LiveClock(Options? options = null,
            IPluralize? pluralize = null,
            IMomentClock? momentClock = null)
        {
            options ??= new Options();
            DefaultUpdatePeriod = options.DefaultUpdatePeriod;
            MaxInvalidationDelay = options.MaxInvalidationDelay;
            Pluralize = options.Pluralize ?? pluralize ?? new Pluralizer();
            Clock = options.Clock ?? momentClock ?? SystemClock.Instance;
        }

        [ComputeMethod]
        public virtual Task<DateTime> GetUtcNow()
        {
            Computed.GetCurrent()!.Invalidate(TrimInvalidationDelay(DefaultUpdatePeriod));
            return Task.FromResult(Clock.Now.ToDateTime());
        }

        [ComputeMethod]
        public virtual Task<DateTime> GetUtcNow(TimeSpan updatePeriod)
        {
            Computed.GetCurrent()!.Invalidate(TrimInvalidationDelay(updatePeriod));
            return Task.FromResult(Clock.Now.ToDateTime());
        }

        [ComputeMethod]
        public virtual Task<string> GetMomentsAgo(DateTime time)
        {
            // TODO: Make this method stop leaking some memory due to timers that don't die unless timeout
            var delta = DateTime.UtcNow - time.DefaultKind(DateTimeKind.Utc).ToUniversalTime();
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;
            var (unit, unitName) = GetMomentsAgoUnit(delta);
            var unitCount = (int) (delta.TotalSeconds / unit.TotalSeconds);
            string result;
            if (unitCount == 0 && unit == TimeSpan.FromSeconds(1))
                result = $"just now";
            else {
                var pluralizedUnitName = Pluralize.Format(unitName, unitCount);
                result = $"{unitCount} {pluralizedUnitName} ago";
            }

            // Invalidate the result when it's supposed to change
            var delay = TrimInvalidationDelay(unit.Multiply(unitCount + 1) - delta + TimeSpan.FromMilliseconds(100));
            Computed.GetCurrent()!.Invalidate(delay, false);
            return Task.FromResult(result);
        }

        private static (TimeSpan Unit, string UnitName) GetMomentsAgoUnit(TimeSpan delta)
        {
            if (delta.TotalSeconds < 60)
                return (TimeSpan.FromSeconds(1), "second");
            if (delta.TotalMinutes < 60)
                return (TimeSpan.FromMinutes(1), "minute");
            if (delta.TotalHours < 24)
                return (TimeSpan.FromHours(1), "hour");
            if (delta.TotalDays < 7)
                return (TimeSpan.FromDays(1), "day");
            return (TimeSpan.FromDays(7), "week");
        }

        private TimeSpan TrimInvalidationDelay(TimeSpan delay)
            => TimeSpanEx.Min(delay, MaxInvalidationDelay);
    }
}
