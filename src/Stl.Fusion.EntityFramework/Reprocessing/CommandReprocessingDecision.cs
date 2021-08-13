using System;

namespace Stl.Fusion.EntityFramework.Reprocessing
{
    public record CommandReprocessingDecision(
        bool ShouldReprocess,
        TimeSpan ReprocessingDelay = default)
    {
        public CommandReprocessingDecision() : this(true) { }
    }
}
