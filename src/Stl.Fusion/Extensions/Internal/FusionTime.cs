namespace Stl.Fusion.Extensions.Internal;

public class FusionTime : IFusionTime
{
    public record Options
    {
        public TimeSpan DefaultUpdatePeriod { get; init; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxInvalidationDelay { get; init; } = TimeSpan.FromMinutes(10);
        public IMomentClock? Clock { get; init; }
    }

    public Options Settings { get; }
    public IMomentClock Clock { get; }

    public FusionTime(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Clock = Settings.Clock ?? services.Clocks().SystemClock;
    }

    public virtual Task<DateTime> GetUtcNow()
    {
        Computed.GetCurrent()!.Invalidate(TrimInvalidationDelay(Settings.DefaultUpdatePeriod));
        return Task.FromResult(Clock.Now.ToDateTime());
    }

    public virtual Task<DateTime> GetUtcNow(TimeSpan updatePeriod)
    {
        Computed.GetCurrent()!.Invalidate(TrimInvalidationDelay(updatePeriod));
        return Task.FromResult(Clock.Now.ToDateTime());
    }

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
            unitName = MaybePluralize(unitName, unitCount);
            result = $"{unitCount} {unitName} ago";
        }

        // Invalidate the result when it's supposed to change
        var delay = TrimInvalidationDelay(unit.Multiply(unitCount + 1) - delta + TimeSpan.FromMilliseconds(100));
        Computed.GetCurrent()!.Invalidate(delay, false);
        return Task.FromResult(result);
    }

    // Protected methods

    protected virtual string MaybePluralize(string word, int count)
        => count == 1 ? word : word + "s"; // Override this in your own descendant :)

    protected virtual (TimeSpan Unit, string SingularUnitName) GetMomentsAgoUnit(TimeSpan delta)
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

    protected virtual TimeSpan TrimInvalidationDelay(TimeSpan delay)
        => TimeSpanExt.Min(delay, Settings.MaxInvalidationDelay);
}
