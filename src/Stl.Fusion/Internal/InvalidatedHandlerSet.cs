namespace Stl.Fusion.Internal;

public struct InvalidatedHandlerSet
{
    private const int ListSize = 5;

    private object? _storage;

    public InvalidatedHandlerSet(Action<IComputed> item)
        => _storage = item;

    public InvalidatedHandlerSet(IEnumerable<Action<IComputed>> items)
    {
        foreach (var item in items)
            Add(item);
    }

    public void Add(Action<IComputed> item)
    {
        if (ReferenceEquals(item, null))
            return;

        switch (_storage) {
            case null:
                _storage = item;
                return;

            case Action<IComputed> anotherItem:
                if (anotherItem == item)
                    return;

                var newList = new Action<IComputed>[ListSize];
                newList[0] = anotherItem;
                newList[1] = item;
                _storage = newList;
                return;

            case Action<IComputed>[] list:
                for (var i = 0; i < list.Length; i++) {
                    var listItem = list[i];
                    if (ReferenceEquals(listItem, null)) {
                        list[i] = item;
                        return;
                    }
                    if (listItem == item)
                        return;
                }
                _storage = new HashSet<Action<IComputed>>(list) { item };
                return;

            case HashSet<Action<IComputed>> set:
                set.Add(item);
                return;

            default:
                throw Stl.Internal.Errors.InternalError($"{GetType().GetName()} structure is corrupted.");
        }
    }

    public void Remove(Action<IComputed> item)
    {
        if (ReferenceEquals(item, null))
            return;

        switch (_storage) {
            case null:
                return;

            case Action<IComputed> anotherItem:
                if (anotherItem == item)
                    _storage = null;
                return;

            case Action<IComputed>[] list:
                for (var i = 0; i < list.Length; i++) {
                    var listItem = list[i];
                    if (ReferenceEquals(listItem, null))
                        return;

                    if (listItem == item) {
                        list.AsSpan(i + 1).CopyTo(list.AsSpan(i));
                        list[^1] = null!;
                        return;
                    }
                }
                return;

            case HashSet<Action<IComputed>> set:
                set.Remove(item);
                return;

            default:
                throw Stl.Internal.Errors.InternalError($"{GetType().GetName()} structure is corrupted.");
        }
    }

    public void Clear()
        => _storage = null;

    public void Invoke(IComputed computed)
    {
        switch (_storage) {
            case null:
                return;

            case Action<IComputed> item:
                item.Invoke(computed);
                return;

            case Action<IComputed>[] list:
                foreach (var item in list) {
                    if (ReferenceEquals(item, null))
                        return;

                    item.Invoke(computed);
                }
                return;

            case HashSet<Action<IComputed>> set:
                foreach (var item in set)
                    item.Invoke(computed);
                return;

            default:
                throw Stl.Internal.Errors.InternalError($"{GetType().GetName()} structure is corrupted.");
        }
    }
}
