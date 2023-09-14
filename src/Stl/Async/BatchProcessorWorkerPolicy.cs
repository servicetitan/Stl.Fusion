using Stl.OS;

namespace Stl.Async;

public interface IBatchProcessorWorkerPolicy
{
    int MinWorkerCount { get; }
    int MaxWorkerCount { get; }

    TimeSpan Cooldown { get; }
    TimeSpan CollectorCycle { get; }

    int GetWorkerCountDelta(TimeSpan minQueueTime);
}

public record BatchProcessorWorkerPolicy : IBatchProcessorWorkerPolicy
{
    public static IBatchProcessorWorkerPolicy Default { get; set; } = new BatchProcessorWorkerPolicy();

    public int MinWorkerCount { get; init; } = 1;
    public int MaxWorkerCount { get; init; } = HardwareInfo.GetProcessorCountFactor(2);

    public TimeSpan KillWorkerAt { get; init; } = TimeSpan.FromMilliseconds(1);
    public TimeSpan Kill8WorkersAt { get; init; } = TimeSpan.FromMilliseconds(0.1);
    public TimeSpan AddWorkerAt { get; init; } = TimeSpan.FromMilliseconds(20);
    public TimeSpan Add4WorkersAt { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan Add8WorkersAt { get; init; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan CollectorCycle { get; set; } = TimeSpan.FromSeconds(5);

    public virtual int GetWorkerCountDelta(TimeSpan minQueueTime)
    {
        if (minQueueTime > Add8WorkersAt)
            return 8;
        if (minQueueTime > Add4WorkersAt)
            return 4;
        if (minQueueTime > AddWorkerAt)
            return 1;
        if (minQueueTime < Kill8WorkersAt)
            return -8;
        if (minQueueTime < KillWorkerAt)
            return -1;
        return 0;
    }
}
