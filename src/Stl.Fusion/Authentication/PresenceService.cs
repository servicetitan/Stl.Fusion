namespace Stl.Fusion.Authentication;

public class PresenceService : WorkerBase
{
    public record Options
    {
        public RandomTimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMinutes(3).ToRandom(0.05);
        public MomentClockSet? Clocks { get; set; }
    }

    protected Options Settings { get; }
    protected ILogger Log { get; }

    protected IAuth Auth { get; }
    protected ISessionResolver SessionResolver { get; }
    protected MomentClockSet Clocks { get; }

    public PresenceService(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Log = services.LogFor(GetType());

        Auth = services.GetRequiredService<IAuth>();
        SessionResolver = services.GetRequiredService<ISessionResolver>();
        Clocks = Settings.Clocks ?? services.Clocks();
    }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
        var retryCount = 0;
        while (!cancellationToken.IsCancellationRequested) {
            var updatePeriod = Settings.UpdatePeriod.Next();
            await Clocks.CpuClock.Delay(updatePeriod, cancellationToken).ConfigureAwait(false);
            var success = await UpdatePresence(session, cancellationToken).ConfigureAwait(false);
            retryCount = success ? 0 : 1 + retryCount;
        }
    }

    protected virtual async Task<bool> UpdatePresence(Session session, CancellationToken cancellationToken)
    {
        try {
            await Auth.UpdatePresence(session, cancellationToken).ConfigureAwait(false);
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
