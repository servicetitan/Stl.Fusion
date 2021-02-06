using System;
using System.Threading;
using Stl.Collections;

namespace Stl.Fusion.Operations.Internal
{
    public class LocalOperation : IOperation
    {
        private static long _operationId;

        public string Id { get; set; } = "";
        public string AgentId { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime CommitTime { get; set; }
        public object? Command { get; set; }
        public OptionSet Items { get; set; } = new();

        public LocalOperation() { }
        public LocalOperation(bool autogenerateId)
        {
            if (autogenerateId)
                Id = "Local-" + Interlocked.Increment(ref _operationId).ToString();
        }
    }
}
