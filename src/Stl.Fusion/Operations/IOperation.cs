using System;
using Stl.Collections;

namespace Stl.Fusion.Operations
{
    public interface IOperation
    {
        string Id { get; set; }
        DateTime StartTime { get; set; } // Always UTC
        DateTime CommitTime { get; set; } // Always UTC
        string AgentId { get; set; }
        object? Command { get; set; }
        ImmutableOptionSet InvalidationData { get; set; }
    }
}
