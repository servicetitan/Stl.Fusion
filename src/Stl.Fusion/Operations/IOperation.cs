using System;
using Stl.Collections;
using Stl.CommandR;

namespace Stl.Fusion.Operations
{
    public interface IOperation
    {
        string Id { get; set; }
        DateTime StartTime { get; set; } // Always UTC
        DateTime CommitTime { get; set; } // Always UTC
        string AgentId { get; set; }
        object? Command { get; set; }
        ImmutableOptionSet Items { get; set; }
    }
}
