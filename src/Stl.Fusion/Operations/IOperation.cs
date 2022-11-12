namespace Stl.Fusion.Operations;

public interface IOperation : IRequirementTarget
{
    string Id { get; set; }
    string AgentId { get; set; }
    DateTime StartTime { get; set; } // Always UTC
    DateTime CommitTime { get; set; } // Always UTC
    object? Command { get; set; }
    OptionSet Items { get; set; }
}
