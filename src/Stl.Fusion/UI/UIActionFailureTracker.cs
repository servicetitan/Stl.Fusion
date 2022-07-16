namespace Stl.Fusion.UI;

public class UIActionFailureTracker : MutableList<IUIActionResult>
{
    protected UIActionFailureTracker() { }
    public UIActionFailureTracker(UIActionTracker uiActionTracker)
    {
        // !!! This task will run till the moment UIActionTracker is disposed
        Task.Run(async () => {
            var failures = uiActionTracker.Results.Where(e => e.HasError);
            await foreach (var failure in failures.ConfigureAwait(false))
                Add(failure);
        });
    }

    public override string ToString()
        => $"{GetType().Name}({Count} item(s))";
}
