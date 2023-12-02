namespace Stl.Fusion.UI;

public class UIActionFailureTracker : MutableList<IUIActionResult>
{
    public record Options
    {
        public TimeSpan MaxDuplicateRecency { get; init; } = TimeSpan.FromSeconds(1);
    }

    private ILogger? _log;

    protected IServiceProvider Services { get; }
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public Options Settings  { get; }
    public Task? WhenRunning { get; protected set; }

    public UIActionFailureTracker(Options settings, IServiceProvider services)
        : this(settings, services, true)
    { }

    protected UIActionFailureTracker(Options settings, IServiceProvider services, bool mustStart)
    {
        Settings = settings;
        Services = services;
        if (mustStart)
            Start();
    }

    public override string ToString()
        => $"{GetType().GetName()}({Count} item(s))";

    public void Start()
        => WhenRunning ??= Run();

    // Protected methods

    protected virtual async Task Run()
    {
        var uiActionTracker = Services.GetRequiredService<UIActionTracker>();
        var cancellationToken = uiActionTracker.StopToken;
        var lastResultEvent = uiActionTracker.LastResult;
        while (true) {
            try {
                lastResultEvent = await lastResultEvent.WhenNext(cancellationToken).ConfigureAwait(false);
                TryAddFailure(lastResultEvent.Value);
            }
            catch (Exception e) {
                if (cancellationToken.IsCancellationRequested)
                    return;

                Log.LogError(e, "Run() method failed, will retry");
                // We don't want it to consume 100% CPU in case of a weird failure, so...
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected virtual bool TryAddFailure(IUIActionResult? result)
    {
        if (result is not { HasError: true })
            return false;

        if (Settings.MaxDuplicateRecency <= TimeSpan.Zero) {
            Add(result);
            return true;
        }

        return Update((result, Settings), static (state, items) => {
            var (newItem, settings) = state;
            var error = newItem.Error;
            if (error == null)
                return items;

            // We don't want to add duplicate exceptions here
            var minCompletedAt = newItem.CompletedAt - settings.MaxDuplicateRecency;
            foreach (var item in items) {
                if (item.CompletedAt < minCompletedAt)
                    continue;
                if (item.Error is not { } e)
                    continue;
                if (e.GetType() != error.GetType())
                    continue;
                if (!Equals(e.Message, error.Message))
                    continue;

                // There is a recent error of the same type & having the same Message
                return items;
            }

            return items.Add(newItem);
        });
    }
}
