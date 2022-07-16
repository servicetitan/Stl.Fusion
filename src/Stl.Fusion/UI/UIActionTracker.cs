namespace Stl.Fusion.UI;

public class UIActionTracker : IDisposable
{
    private static readonly UnboundedChannelOptions ChannelOptions =
        new() { AllowSynchronousContinuations = false };

    public static UIActionTracker None { get; } = new NoUIActionTracker(MomentClockSet.Default);

    protected volatile AsyncEventImpl<UIAction?> LastActionEventImpl;
    protected volatile AsyncEventImpl<IUIActionResult?> LastResultEventImpl;

    protected HashSet<Channel<UIAction>> ActionChannels { get; } = new();
    protected HashSet<Channel<IUIActionResult>> ResultChannels { get; } = new();
    protected bool IsDisposed { get; private set; }

    public IAsyncEnumerable<UIAction> Actions => GetActions();
    public IAsyncEnumerable<IUIActionResult> Results => GetResults();
    public AsyncEvent<UIAction?> LastActionEvent => LastActionEventImpl;
    public AsyncEvent<IUIActionResult?> LastResultEvent => LastResultEventImpl;
    public MomentClockSet Clocks { get; }

    public UIActionTracker(MomentClockSet clocks)
    {
        LastActionEventImpl = new AsyncEventImpl<UIAction?>(null, true);
        LastResultEventImpl = new AsyncEventImpl<IUIActionResult?>(null, true);
        Clocks = clocks;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Intentionally ignore disposing flag here
        lock (ActionChannels) {
            lock (ResultChannels) {
                if (IsDisposed)
                    return;
                IsDisposed = true;
                foreach (var channel in ActionChannels)
                    channel.Writer.Complete();
                ActionChannels.Clear();
                foreach (var channel in ResultChannels)
                    channel.Writer.Complete();
                ResultChannels.Clear();
            }
        }
    }

    public virtual void Register(UIAction action)
    {
        lock (ActionChannels) {
            if (IsDisposed)
                return;
            var prevEvent = LastActionEventImpl;
            var nextEvent = prevEvent.CreateNext(action);
            LastActionEventImpl = nextEvent;
            prevEvent.Complete(nextEvent);
            foreach (var channel in ActionChannels)
                channel.Writer.TryWrite(action);
        }
        action.WhenCompleted().ContinueWith(_ => {
            var result = action.UntypedResult;
            if (result == null)
                return;
            lock (ResultChannels) {
                if (IsDisposed)
                    return;
                var prevEvent = LastResultEventImpl;
                var nextEvent = prevEvent.CreateNext(result);
                LastResultEventImpl = nextEvent;
                prevEvent.Complete(nextEvent);
                foreach (var channel in ResultChannels)
                    channel.Writer.TryWrite(result);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    // Protected methods

    protected virtual async IAsyncEnumerable<UIAction> GetActions(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<UIAction>(ChannelOptions);
        lock (ActionChannels) {
            if (IsDisposed)
                yield break;
            ActionChannels.Add(channel);
        }
        var reader = channel.Reader;
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out var item))
            yield return item;
        lock (ActionChannels) {
            ActionChannels.Remove(channel);
        }
    }

    protected virtual async IAsyncEnumerable<IUIActionResult> GetResults(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<IUIActionResult>(ChannelOptions);
        lock (ResultChannels) {
            if (IsDisposed)
                yield break;
            ResultChannels.Add(channel);
        }
        var reader = channel.Reader;
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out var item))
            yield return item;
        lock (ResultChannels) {
            ResultChannels.Remove(channel);
        }
    }

    // Nested types

    private class NoUIActionTracker : UIActionTracker
    {
        public NoUIActionTracker(MomentClockSet clocks) 
            : base(clocks) { }

        public override void Register(UIAction action) 
        { }
    }
}
