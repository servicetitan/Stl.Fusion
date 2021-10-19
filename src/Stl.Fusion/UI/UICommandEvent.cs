using System.Text.Json.Serialization;

namespace Stl.Fusion.UI;

public record UICommandEvent
{
    private static long _nextCommandId;

    public long CommandId { get; init; } = Interlocked.Increment(ref _nextCommandId);
    public ICommand Command { get; init; } = null!;

    public Moment? CreatedAt { get; init; }
    public Moment? CompletedAt { get; init; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public TimeSpan? Duration => CompletedAt - CreatedAt;

    public IResult? Result { get; init; }
    public bool IsCompleted => Result != null;
    public bool IsCompletedSuccessfully => Result?.HasValue ?? false;
    public bool IsFailed => Result?.Error != null;

    public UICommandEvent() { }
    public UICommandEvent(ICommand command, IResult? result = null)
    {
        Command = command;
        Result = result;
    }
}
