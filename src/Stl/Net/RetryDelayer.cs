namespace Stl.Net;

public class RetryDelayer : IRetryDelayer
{
    private CancellationTokenSource _cancelDelaysCts = new();

    protected object Lock = new();

    public IMomentClock Clock { get; init; } = CpuClock.Instance;
    public RetryDelaySeq Delays { get; set; } = new();
    public int? Limit { get; set; }

    public CancellationToken CancelDelaysToken { get; private set; }

    public virtual RetryDelay GetDelay(int tryIndex, CancellationToken cancellationToken = default)
    {
        if (Limit is { } limit && tryIndex >= limit)
            throw RetryLimitExceededError(limit);

        var delay = Delays[tryIndex];
        if (tryIndex == 0 || delay <= TimeSpan.Zero)
            return RetryDelay.None;

        delay = TimeSpanExt.Max(TimeSpan.FromMilliseconds(1), delay);
        return (DelayImpl(), Clock.Now + delay);

        async Task DelayImpl()
        {
            var cancelDelaysToken = CancelDelaysToken;
            var cts = cancellationToken.LinkWith(cancelDelaysToken);
            try {
                await Clock.Delay(delay, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancelDelaysToken.IsCancellationRequested) {
                // We complete normally in this case
            }
            finally {
                cts.Dispose();
            }
        }
    }

    public virtual void CancelDelays()
    {
        CancellationTokenSource cts;
        lock (Lock) {
            cts = _cancelDelaysCts;
            _cancelDelaysCts = new();
            CancelDelaysToken = _cancelDelaysCts.Token;
        }
        cts.CancelAndDisposeSilently();
    }

    // Protected methods

    protected virtual Exception RetryLimitExceededError(int limit)
        => new RetryLimitExceededException();
}
