using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion.Authentication;

public class PresenceService : AsyncProcessBase
{
    public class Options
    {
        public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMinutes(3);
        public MomentClockSet? Clocks { get; set; }
    }

    protected TimeSpan UpdatePeriod { get; }
    protected IAuthService AuthService { get; }
    protected ISessionResolver SessionResolver { get; }
    protected MomentClockSet Clocks { get; }
    protected ILogger Log { get; }

    public PresenceService(
        Options? options,
        IServiceProvider services,
        ILogger<PresenceService>? log = null)
    {
        options ??= new();
        Log = log ?? NullLogger<PresenceService>.Instance;
        UpdatePeriod = options.UpdatePeriod;
        Clocks = options.Clocks ?? services.Clocks();
        AuthService = services.GetRequiredService<IAuthService>();
        SessionResolver = services.GetRequiredService<ISessionResolver>();
    }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
        var retryCount = 0;
        while (!cancellationToken.IsCancellationRequested) {
            await Clocks.CoarseCpuClock.Delay(UpdatePeriod, cancellationToken).ConfigureAwait(false);
            var success = await UpdatePresence(session, cancellationToken).ConfigureAwait(false);
            retryCount = success ? 0 : 1 + retryCount;
        }
    }

    protected virtual async Task<bool> UpdatePresence(Session session, CancellationToken cancellationToken)
    {
        try {
            await AuthService.UpdatePresence(session, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception e) {
            Log.LogError(e, "UpdatePresenceAsync error");
            return false;
        }
    }
}
