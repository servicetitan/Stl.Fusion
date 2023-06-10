using System.Collections.ObjectModel;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcConfiguration
{
    private readonly object _lock = new();
    private IDictionary<Type, RpcServiceBuilder> _services;
    private IDictionary<Symbol, Type> _inboundCallTypes;
    private Func<Type, Symbol> _serviceNameBuilder = DefaultServiceNameBuilder;
    private Func<RpcMethodDef, Symbol> _methodNameBuilder = DefaultMethodNameBuilder;
    private Func<Type, Symbol, bool> _backendServiceDetector = DefaultBackendServiceDetector;

    private RpcArgumentSerializer _argumentSerializer = RpcArgumentSerializer.Default;

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

    public Func<Type, Symbol> ServiceNameBuilder {
        get => _serviceNameBuilder;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _serviceNameBuilder = value;
        }
    }

    public Func<RpcMethodDef, Symbol> MethodNameBuilder {
        get => _methodNameBuilder;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _methodNameBuilder = value;
        }
    }

    public Func<Type, Symbol, bool> BackendServiceDetector {
        get => _backendServiceDetector;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _backendServiceDetector = value;
        }
    }

    public RpcArgumentSerializer ArgumentSerializer {
        get => _argumentSerializer;
        set {
            if (IsFrozen)
                throw Errors.AlreadyReadOnly<RpcConfiguration>();
            _argumentSerializer = value;
        }
    }

    public static Symbol DefaultServiceNameBuilder(Type serviceType)
        => serviceType.GetName();

    public static Symbol DefaultMethodNameBuilder(RpcMethodDef methodDef)
        => $"{methodDef.Method.Name}:{methodDef.RemoteParameterTypes.Length}";

    public RpcConfiguration()
    {
        _services = new Dictionary<Type, RpcServiceBuilder>();
        _inboundCallTypes = new Dictionary<Symbol, Type>() {
            { Symbol.Empty, typeof(RpcInboundCall<>) },
        };
    }

    private static bool DefaultBackendServiceDetector(Type serviceType, Symbol serviceName)
        => serviceType.Name.EndsWith("Backend", StringComparison.Ordinal)
        || serviceName.Value.StartsWith("backend.", StringComparison.Ordinal);

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
