namespace Stl.Fusion;

public sealed partial record UpdateDelayer
{
    public sealed record Fixed(
        TimeSpan UpdateDelay,
        RetryDelaySeq RetryDelays
    ) : IUpdateDelayer
    {
        public IMomentClock Clock { get; init; } = MomentClockSet.Default.UIClock;



        public Fixed(double updateDelay)
            : this(TimeSpan.FromSeconds(updateDelay), Defaults.RetryDelays) { }
        public Fixed(double updateDelay, RetryDelaySeq retryDelays)
            : this(TimeSpan.FromSeconds(updateDelay), retryDelays) { }
        public Fixed(TimeSpan updateDelay)
            : this(updateDelay, Defaults.RetryDelays) { }

        public async ValueTask Delay(int retryCount, CancellationToken cancellationToken = default)
        {
            var delay = TimeSpanExt.Max(UpdateDelay, GetDelay(retryCount));
            if (delay <= TimeSpan.Zero)
                return; // This may only happen if MinDelay == 0 - e.g. for UpdateDelayer.ZeroUnsafe

            await Clock.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        public TimeSpan GetDelay(int retryCount)
            => retryCount > 0 ? RetryDelays[retryCount] : UpdateDelay;

        // We want referential equality back for this type:
        // it's a record solely to make it possible to use it with "with" keyword
        public bool Equals(Fixed? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
