using FluentAssertions.Extensions;

namespace Stl.Fusion.Tests.Services;

public interface ITimeServer : IComputeService
{
    [ComputeMethod]
    Task<DateTime> GetTime(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeWithDelay(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<string?> GetFormattedTime(string format, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<DateTime> GetTimeWithOffset(TimeSpan offset);
}

public interface ITimeService : ITimeServer
{
    [ComputeMethod]
    Task<DateTime> GetTimeNoMethod(CancellationToken cancellationToken = default);
}

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
            // MessagePack always deserializes DateTime as UTC,
            // so if it's local, it won't deserialize properly
            var now = DateTime.UtcNow;
            _log.LogDebug($"GetTime() -> {now}");
            return now;
        }
    }

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual Task<DateTime> GetTime(CancellationToken cancellationToken = default)
        => Task.FromResult(Time);

    [ComputeMethod(AutoInvalidationDelay = 0.25)]
    public virtual Task<DateTime> GetTimeNoMethod(CancellationToken cancellationToken = default)
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
