namespace Stl.Fusion.Operations.Internal;

public class TransientOperation : IOperation
{
    private static long _operationId;

    public string Id { get; set; } = "";
    public string AgentId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime CommitTime { get; set; }
    public object? Command { get; set; }
    public OptionSet Items { get; set; } = new();

    public TransientOperation() { }
    public TransientOperation(bool autogenerateId)
    {
        if (autogenerateId)
            Id = "Local-" + Interlocked.Increment(ref _operationId).ToString();
    }
}
