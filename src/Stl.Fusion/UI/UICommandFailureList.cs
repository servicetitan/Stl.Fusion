namespace Stl.Fusion.UI;

public class UICommandFailureList : ICollection<UICommandEvent>
{
    protected object Lock { get; } = new();

    public ImmutableList<UICommandEvent> Items { get; private set; } = ImmutableList<UICommandEvent>.Empty;
    public int Count => Items.Count;
    public event Action? Changed;

    bool ICollection<UICommandEvent>.IsReadOnly => false;

    public UICommandFailureList() { }
    public UICommandFailureList(IUICommandTracker uiCommandTracker)
    {
        // !!! This task will run till the moment commandTracker is disposed
        Task.Run(async () => {
            var failures = uiCommandTracker.Events.Where(e => e.IsFailed);
            await foreach (var failure in failures.ConfigureAwait(false))
                Add(failure);
        });
    }

    public override string ToString()
        => $"{GetType().Name}({Count} item(s))";

    // Read methods

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<UICommandEvent> GetEnumerator() => Items.GetEnumerator();

    public bool Contains(UICommandEvent item)
        => Items.Contains(item);

    public void CopyTo(UICommandEvent[] array, int arrayIndex)
        => Items.CopyTo(array, arrayIndex);

    // Write methods

    public bool Update(Func<ImmutableList<UICommandEvent>, ImmutableList<UICommandEvent>> updater)
    {
        lock (Lock) {
            var newItems = updater.Invoke(Items);
            if (newItems == Items)
                return false;
            Items = newItems;
            Changed?.Invoke();
            return true;
        }
    }

    public void Add(UICommandEvent item)
        => Update(items => items.Add(item));

    public bool Remove(UICommandEvent item)
        => Update(items => items.Remove(item));

    public bool Remove(long actionId)
        => Update(items => items.RemoveAll(i => i.CommandId == actionId));

    public void Clear()
        => Update(_ => ImmutableList<UICommandEvent>.Empty);
}
