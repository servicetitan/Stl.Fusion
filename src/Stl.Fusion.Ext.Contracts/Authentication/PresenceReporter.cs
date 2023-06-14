namespace Stl.Fusion.Authentication;

public class PresenceReporter : WorkerBase
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public RandomTimeSpan UpdatePeriod { get; init; } = TimeSpan.FromMinutes(3).ToRandom(0.05);
        public RetryDelaySeq RetryDelays { get; init; } = new(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        public MomentClockSet? Clocks { get; init; }
    }

    protected Options Settings { get; }
    protected ILogger Log { get; }

    protected IAuth Auth { get; }
    protected ISessionResolver SessionResolver { get; }
    protected MomentClockSet Clocks { get; }

    public PresenceReporter(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Log = services.LogFor(GetType());

        Auth = services.GetRequiredService<IAuth>();
        SessionResolver = services.GetRequiredService<ISessionResolver>();
        Clocks = Settings.Clocks ?? services.Clocks();
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
        var retryCount = 0;
        while (!cancellationToken.IsCancellationRequested) {
            var updatePeriod = retryCount == 0
                ? Settings.UpdatePeriod.Next()
                : Settings.RetryDelays[retryCount];
            await Clocks.CpuClock.Delay(updatePeriod, cancellationToken).ConfigureAwait(false);
            var success = await UpdatePresence(session, cancellationToken).ConfigureAwait(false);
            retryCount = success ? 0 : 1 + retryCount;
        }
    }

    // Private methods

    private async Task<bool> UpdatePresence(Session session, CancellationToken cancellationToken)
    {
        try {
            await Auth.UpdatePresence(session, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "UpdatePresence failed");
            return false;
        }
    }
}
