using Stl.Fusion.UI.Internal;

namespace Stl.Fusion.UI;

public class UIActionTracker : IHasServices, IDisposable
{
    private static readonly UnboundedChannelOptions ChannelOptions =
        new() { AllowSynchronousContinuations = false };

    public static UIActionTracker None { get; set; } = new NoUIActionTracker(0);

    public record Options {
        public TimeSpan MaxInvalidationDelay { get; init; } = TimeSpan.FromMilliseconds(300);
        public IMomentClock? Clock { get; init; }
    }

    protected long RunningActionCountValue;
    protected volatile AsyncEventImpl<UIAction?> LastActionEventImpl;
    protected volatile AsyncEventImpl<IUIActionResult?> LastResultEventImpl;

    protected HashSet<Channel<UIAction>> ActionChannels { get; } = new();
    protected HashSet<Channel<IUIActionResult>> ResultChannels { get; } = new();
    protected bool IsDisposed { get; private set; }

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public IMomentClock Clock { get; }
    public ILogger Log { get; }

    public long RunningActionCount => Interlocked.Read(ref RunningActionCountValue);
    public IAsyncEnumerable<UIAction> Actions => GetActions();
    public IAsyncEnumerable<IUIActionResult> Results => GetResults();
    public AsyncEvent<UIAction?> LastActionEvent => LastActionEventImpl;
    public AsyncEvent<IUIActionResult?> LastResultEvent => LastResultEventImpl;

    public UIActionTracker(Options options, IServiceProvider services)
    {
        Settings = options;
        Services = services;
        Clock = options.Clock ?? services.Clocks().UIClock;
        Log = services.LogFor(GetType());

        LastActionEventImpl = new AsyncEventImpl<UIAction?>(null, true);
        LastResultEventImpl = new AsyncEventImpl<IUIActionResult?>(null, true);
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
                Interlocked.Exchange(ref RunningActionCountValue, 0);
            }
        }
    }

    public virtual void Register(UIAction action)
    {
        lock (ActionChannels) {
            if (IsDisposed)
                return;
            Interlocked.Increment(ref RunningActionCountValue);
            try {
                var prevEvent = LastActionEventImpl;
                var nextEvent = prevEvent.CreateNext(action);
                LastActionEventImpl = nextEvent;
                prevEvent.Complete(nextEvent);
                foreach (var channel in ActionChannels)
                    channel.Writer.TryWrite(action);
            }
            catch (Exception e) {
                // We need to keep this count consistent if above block somehow fails
                Interlocked.Decrement(ref RunningActionCountValue);
                if (e is not OperationCanceledException)
                    Log.LogError("UI action registration failed: {Action}", action);
                throw;
            }
        }
        action.WhenCompleted().ContinueWith(_ => {
            Interlocked.Decrement(ref RunningActionCountValue);
            var result = action.UntypedResult;
            if (result == null) {
                Log.LogError("UI action has completed w/o a result: {Action}", action);
                return;
            }
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
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public bool AreInstantUpdatesEnabled()
    {
        // 1. When any action is running
        if (RunningActionCount > 0)
            return true;

        // 2. When invalidations triggered by the most recent action are still coming
        if (LastResultEvent.Value?.CompletedAt >= Clock.Now - Settings.MaxInvalidationDelay)
            return true;

        return false;
    }

    public Task WhenInstantUpdatesEnabled() 
        => AreInstantUpdatesEnabled() ? Task.CompletedTask : LastActionEvent.WhenNext();

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
}
