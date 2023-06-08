using Stl.OS;

namespace Stl.Channels;

public delegate void ChannelAttachedHandler<T>(Channel<T> channel);
public delegate void ChannelDetachedHandler<T>(Channel<T> channel, ref ArrayBuffer<ValueTask> taskCollector);

public interface IChannelHub<T> : IAsyncDisposable
{
    int ChannelCount { get; }
    bool Attach(Channel<T> channel);
    bool IsAttached(Channel<T> channel);
    ValueTask<bool> Detach(Channel<T> channel);

    event ChannelAttachedHandler<T> Attached;
    event ChannelDetachedHandler<T> Detached;
}

public class ChannelHub<T> : SafeAsyncDisposableBase, IChannelHub<T>
{
    protected ConcurrentDictionary<Channel<T>, Unit> Channels { get; }

    public int ChannelCount => Channels.Count;
    public event ChannelAttachedHandler<T>? Attached;
    public event ChannelDetachedHandler<T>? Detached;

    public ChannelHub()
    {
        var concurrencyLevel = HardwareInfo.GetProcessorCountFactor(4, 4);
        var capacity = 17;
        Channels = new ConcurrentDictionary<Channel<T>, Unit>(concurrencyLevel, capacity);
    }

    public virtual bool Attach(Channel<T> channel)
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel));
        this.ThrowIfDisposedOrDisposing();

        if (!Channels.TryAdd(channel, default))
            return false;
        OnAttached(channel);
        return true;
    }

    public virtual async ValueTask<bool> Detach(Channel<T> channel)
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel));
        this.ThrowIfDisposedOrDisposing();

        if (!Channels.TryRemove(channel, out _))
            return false;
        await OnDetached(channel).ConfigureAwait(false);
        return true;
    }

    public bool IsAttached(Channel<T> channel)
        => Channels.TryGetValue(channel, out _);

    protected virtual void OnAttached(Channel<T> channel)
    {
        _ = channel.Reader.Completion.ContinueWith(
            _ => Detach(channel),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        Attached?.Invoke(channel);
    }

    protected virtual async ValueTask OnDetached(Channel<T> channel)
    {
        var taskCollector = ArrayBuffer<ValueTask>.Lease(true);
        try {
            Detached?.Invoke(channel, ref taskCollector);

            // The traversal direction doesn't matter, so let's traverse
            // it in reverse order to help the compiler to get rid of extra
            // bound checks.
            var tasks = taskCollector.Buffer;
            for (var i = taskCollector.Count - 1; i >= 0; i--) {
                var task = tasks[i];
                try {
                    await task.ConfigureAwait(false);
                }
                catch {
                    // Ignore: we want to invoke all handlers
                }
            }
        }
        finally {
            taskCollector.Release();
        }

        if (channel is IAsyncDisposable ad)
            await ad.DisposeAsync().ConfigureAwait(false);
        channel.Writer.TryComplete();
    }

    protected override async Task DisposeAsync(bool disposing)
    {
        if (!disposing) return;

        while (!Channels.IsEmpty) {
            await Channels
                .Select(p => Task.Run(async () => {
                    var channel = p.Key;
                    if (!Channels.TryRemove(channel, out _))
                        return;
                    try {
                        await OnDetached(channel).ConfigureAwait(false);
                    }
                    catch {
                        // Ignore: we did what we could, Dispose shouldn't throw anything
                    }
                }))
                .Collect()
                .ConfigureAwait(false);
        }
    }
}
