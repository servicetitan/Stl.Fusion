namespace Stl.Fusion.UI;

public class UIActionFailureTracker : MutableList<IUIActionResult>, IHasServices
{
    public record Options
    {
        public TimeSpan MaxDuplicateRecency { get; init; } = TimeSpan.FromSeconds(1);
    }

    public IServiceProvider Services { get; }
    public Options Settings  { get; }
    public Task WhenTracking { get; protected init; } = null!;

    public UIActionFailureTracker(Options settings, IServiceProvider services)
        : this(settings, services, true)
    { }

    protected UIActionFailureTracker(Options settings, IServiceProvider services, bool mustStart)
    {
        Settings = settings;
        Services = services;
        if (mustStart)
            // ReSharper disable once VirtualMemberCallInConstructor
            WhenTracking = TrackFailures();
    }

    public override string ToString()
        => $"{GetType().GetName()}({Count} item(s))";

    protected virtual async Task TrackFailures()
    {
        var uiActionTracker = Services.GetRequiredService<UIActionTracker>();
        var cancellationToken = uiActionTracker.StopToken;

        var lastResultEvent = uiActionTracker.LastResultEvent;
        while (true) {
            lastResultEvent = await lastResultEvent.WhenNext(cancellationToken).ConfigureAwait(false);
            if (lastResultEvent == null)
                return;

            var result = lastResultEvent.Value;
            if (result != null)
                TryAddFailure(result);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    protected virtual bool TryAddFailure(IUIActionResult result)
    {
        if (!result.HasError)
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
