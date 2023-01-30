using Stl.OS;

namespace Stl.Caching;

public class RefHolder
{
    private readonly LinkedList<object>[] _lists;
    private readonly int _concurrencyMask;

    public bool IsEmpty {
        get {
            foreach (var list in _lists) {
                lock (list) {
                    if (list.First != null)
                        return false;
                }
            }
            return true;
        }
    }

    public RefHolder(int concurrencyLevel = -1)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(4);
        concurrencyLevel =  Math.Max(1, (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel));
        _concurrencyMask = concurrencyLevel - 1;
        _lists = new LinkedList<object>[concurrencyLevel];
        for (var i = 0; i < _lists.Length; i++)
            _lists[i] = new LinkedList<object>();
    }

    public ClosedDisposable<(LinkedList<object>, LinkedListNode<object>)> Hold(object obj)
        => Hold(obj, RuntimeHelpers.GetHashCode(obj));

    public ClosedDisposable<(LinkedList<object>, LinkedListNode<object>)> Hold(object obj, int random)
    {
        var list = _lists[random & _concurrencyMask];
        LinkedListNode<object> node;
        lock (list)
            node = list.AddLast(obj);
        return Disposable.NewClosed((list, node), state => {
            var (list1, node1) = state;
            lock (list1)
                list1.Remove(node1);
        });
    }
}
