using System;
using Stl.Collections;

namespace Stl.Fusion.Operations
{
    public interface IOperation
    {
        string Id { get; set; }
        string AgentId { get; set; }
        DateTime StartTime { get; set; } // Always UTC
        DateTime CommitTime { get; set; } // Always UTC
        object? Command { get; set; }
        ImmutableOptionSet Items { get; set; }
    }
}
