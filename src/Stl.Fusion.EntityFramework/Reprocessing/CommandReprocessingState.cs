using System;
using Microsoft.EntityFrameworkCore;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Reprocessing
{
    public record CommandReprocessingState
    {
        public static Generator<long> Rng = new RandomInt32Generator();

        public int MaxAttemptCount { get; init; } = 3;
        public int FailureCount { get; init; }
        public Exception? Error { get; init; }
        public CommandReprocessingDecision Decision { get; init; } = new(true);

        public virtual CommandReprocessingDecision ComputeDecision()
        {
            if (FailureCount >= MaxAttemptCount)
                return new(false);
            if (Error is not DbUpdateException)
                return new(false);
            var delay = TimeSpan.FromMilliseconds(10 + Math.Abs(Rng.Next() % 100));
            return new(true, delay);
        }
    }
}
