using System.Collections.ObjectModel;
using Stl.Internal;

namespace Stl.Rpc;

public class RpcConfiguration
{
    private readonly object _lock = new();
    private IDictionary<Type, RpcServiceBuilder> _services = new Dictionary<Type, RpcServiceBuilder>();

    public bool IsFrozen { get; private set; }

    public IDictionary<Type, RpcServiceBuilder> Services {
        get => _services;
        set {
            AssertNotFrozen();
            _services = value;
        }
    }

    public void Freeze()
    {
        if (IsFrozen)
            return;

        lock (_lock) {
            if (IsFrozen) // Double-check locking
                return;

            IsFrozen = true;
            _services = new ReadOnlyDictionary<Type, RpcServiceBuilder>(Services);
        }
    }

    // Protected methods

    protected void AssertNotFrozen()
    {
        if (IsFrozen)
            throw Errors.AlreadyReadOnly<RpcConfiguration>();
    }
}
