namespace Stl.Fusion.Tests.Services;

public interface ITimeService : IComputeService
{
    [ComputeMethod]
    Task<DateTime> GetTime(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeNoControllerMethod(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeNoPublication(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeWithDelay(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<string?> GetFormattedTime(string format, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeWithOffset(TimeSpan offset);
}

[RegisterComputeService(typeof(ITimeService), Scope = ServiceScope.Services)]
public class TimeService : ITimeService
{
    private readonly ILogger _log;
    protected bool IsCaching { get; }

    public TimeService(ILogger<TimeService>? log = null)
    {
        _log = log ?? NullLogger<TimeService>.Instance;
        IsCaching = GetType().Name.EndsWith("Proxy");
    }

    public DateTime Time {
        get {
            var now = DateTime.Now;
            _log.LogDebug($"GetTime() -> {now}");
            return now;
        }
    }

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual Task<DateTime> GetTime(CancellationToken cancellationToken = default)
        => Task.FromResult(Time);

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual Task<DateTime> GetTimeNoControllerMethod(CancellationToken cancellationToken = default) 
        => Task.FromResult(Time);

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual Task<DateTime> GetTimeNoPublication(CancellationToken cancellationToken = default)
        => Task.FromResult(Time);

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual async Task<DateTime> GetTimeWithDelay(CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        return Time;
    }

    [ComputeMethod]
    public virtual async Task<string?> GetFormattedTime(string format, CancellationToken cancellationToken = default)
    {
        var time = await GetTime(cancellationToken).ConfigureAwait(false);
        var result = string.Format(format, time);
        return result == "null" ? null : result;
    }

    public virtual async Task<DateTime> GetTimeWithOffset(TimeSpan offset)
    {
        var now = await GetTime().ConfigureAwait(false);
        return now + offset;
    }
}
