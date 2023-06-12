using System.Collections.ObjectModel;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcConfiguration
{
    private readonly object _lock = new();
    private IDictionary<Type, RpcServiceBuilder> _services;
    private IDictionary<Symbol, Type> _inboundCallTypes;

    public bool IsFrozen { get; private set; }

    public IDictionary<Type, RpcServiceBuilder> Services {
        get => _services;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _services = value;
        }
    }

    public IDictionary<Symbol, Type> InboundCallTypes {
        get => _inboundCallTypes;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _inboundCallTypes = value;
        }
    }

    public RpcConfiguration()
    {
        _services = new Dictionary<Type, RpcServiceBuilder>();
        _inboundCallTypes = new Dictionary<Symbol, Type>() {
            { Symbol.Empty, typeof(RpcInboundCall<>) },
        };
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
            _inboundCallTypes = new ReadOnlyDictionary<Symbol, Type>(InboundCallTypes);
        }
    }
}
