using System;

namespace Stl.Fusion.EntityFramework.Reprocessing
{
    public record CommandReprocessingState(int FailureCount)
    {
        public Exception? LastError { get; init; }

        public CommandReprocessingState() : this(0) { }

        public virtual CommandReprocessingState Next(Exception error)
            => this with {
                FailureCount = FailureCount + 1,
                LastError = error
            };
    }
}
