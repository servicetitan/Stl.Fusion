namespace Stl.Fusion.UI;

public interface IUIActionResult : IResult, IHasId<long>
{
    UIAction UntypedAction { get; }
    ICommand Command { get; }
    Moment StartedAt { get; }
    Moment CompletedAt { get; }
    TimeSpan Duration { get; }
    CancellationToken CancellationToken { get; }
}

public class UIActionResult<TResult> : ResultBox<TResult>, IUIActionResult
{
    public UIAction<TResult> Action { get; }
    public Moment CompletedAt { get; }

    // Computed properties
    public UIAction UntypedAction => Action;
    public long Id => Action.Id;
    public ICommand Command => Action.Command;
    public Moment StartedAt => Action.StartedAt;
    public TimeSpan Duration => CompletedAt - StartedAt;
    public CancellationToken CancellationToken => Action.CancellationToken;

    public UIActionResult(UIAction<TResult> action, Result<TResult> result, Moment completedAt)
        : base(result)
    {
        Action = action;
        CompletedAt = completedAt;
    }

    // Conversion

    public override string ToString()
        => $"{GetType().Name}(#{Id}: {AsResult()}, Duration = {Duration.ToShortString()})";
}
