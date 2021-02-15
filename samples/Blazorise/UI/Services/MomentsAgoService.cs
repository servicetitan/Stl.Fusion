using System;
using System.Threading.Tasks;
using Pluralize.NET;
using Stl.Fusion;
using Stl.Time;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.UI.Services
{
    [ComputeService(typeof(IMomentsAgoService))]
    public class MomentsAgoService : IMomentsAgoService
    {
        private readonly IPluralize _pluralize;

        public MomentsAgoService(IPluralize pluralize) => _pluralize = pluralize;

        [ComputeMethod]
        public virtual Task<string> GetMomentsAgoAsync(DateTime time)
        {
            // TODO: Make this method stop leaking some memory due to timers that don't die unless timeout
            var delta = DateTime.UtcNow - time.ToUniversalTime();
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;
            var (unit, unitName) = GetMomentsAgoUnit(delta);
            var unitCount = (int) (delta.TotalSeconds / unit.TotalSeconds);
            var pluralizedUnitName = _pluralize.Format(unitName, unitCount);
            var result = $"{unitCount} {pluralizedUnitName} ago";

            // Invalidate the result when it's supposed to change
            var delay = (unitCount + 1) * unit - delta;
            delay = TimeSpanEx.Min(delay, TimeSpan.FromMinutes(10)); // A sort of mem leak prevention
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
    }
}
