using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Client.Interception;

public abstract class ClientComputedCache
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public IByteSerializer Serializer { get; init; } = ByteSerializer.Default;
        public TimeSpan FlushDelay { get; init; } = TimeSpan.FromSeconds(0.1);
        public IMomentClock? Clock { get; init; }
    }

    protected readonly Options Settings;
    protected readonly IServiceProvider Services;
    protected readonly IMomentClock Clock;
    protected readonly IByteSerializer Serializer;
    protected ILogger Log;

    protected readonly object Lock = new();
    protected Dictionary<ComputeMethodInput, IClientComputed?> FlushQueue = new();
    protected Task? FlushTask;

    protected ClientComputedCache(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = services.LogFor(GetType());
        Clock = settings.Clock ?? services.Clocks().CpuClock;
        Serializer = settings.Serializer;
    }

    public async ValueTask<Result<T>?> Get<T>(ComputeMethodInput input, CancellationToken cancellationToken)
    {
        var cacheBehavior = input.MethodDef.ComputedOptions.ClientCacheBehavior;
        if (cacheBehavior == ClientCacheBehavior.NoCache)
            return null;

        lock (Lock) {
            if (FlushQueue.TryGetValue(input, out var computed))
                return computed is ClientComputed<T> vComputed ? vComputed.Output : null;
        }

        try {
            return await Read<T>(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.LogError(e, "Get({Input}) failed", input);
            return null;
        }
    }

    public void Set(IClientComputed computed)
    {
        var input = (ComputeMethodInput)computed.Input;
        var cacheBehavior = input.MethodDef.ComputedOptions.ClientCacheBehavior;
        if (cacheBehavior == ClientCacheBehavior.NoCache)
            return;

        lock (Lock) {
            FlushQueue[input] = computed;
            FlushTask ??= DelayedFlush();
        }
    }

    public void Remove(ComputeMethodInput input)
    {
        var cacheBehavior = input.MethodDef.ComputedOptions.ClientCacheBehavior;
        if (cacheBehavior == ClientCacheBehavior.NoCache)
            return;

        lock (Lock) {
            FlushQueue[input] = null;
            FlushTask ??= DelayedFlush();
        }
    }

    protected async Task DelayedFlush()
    {
        await Clock.Delay(Settings.FlushDelay).ConfigureAwait(false);
        Dictionary<ComputeMethodInput, IClientComputed?> flushQueue;
        lock (Lock) {
            flushQueue = FlushQueue;
            FlushQueue = new();
            FlushTask = null;
        }
        await Flush(flushQueue).ConfigureAwait(false);
    }

    protected async Task Flush(Dictionary<ComputeMethodInput, IClientComputed?> flushQueue)
    {
        var batch = new List<(byte[], byte[])>();
        using var buffer = new ArrayPoolBufferWriter<byte>();
        foreach (var (input, computed) in flushQueue) {
            // TBD
        }
    }

    protected abstract ValueTask<Result<T>?> Read<T>(ComputeMethodInput input, CancellationToken cancellationToken);
}
