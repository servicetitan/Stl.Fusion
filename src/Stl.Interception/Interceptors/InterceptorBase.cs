using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public abstract class InterceptorBase : IOptionalInterceptor, IHasServices
{
    public record Options
    {
        public bool IsLoggingEnabled { get; init; } = true;
    }

    private static readonly MethodInfo CreateTypedHandlerMethod = typeof(InterceptorBase)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateHandler)));

    private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandlerUntyped;
    private readonly Func<MethodInfo, IInvocation, MethodDef?> _createMethodDef;
    private readonly ConcurrentDictionary<MethodInfo, MethodDef?> _methodDefCache = new();
    private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache = new();
    private readonly ConcurrentDictionary<Type, Unit> _validateTypeCache = new();

    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Debug;
    protected LogLevel ValidationLogLevel { get; set; } = LogLevel.Debug;

    public IServiceProvider Services { get; }

    protected InterceptorBase(Options options, IServiceProvider services)
    {
        Services = services;
        Log = Services.LogFor(GetType());
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsLogging(LogLevel);

        _createHandlerUntyped = CreateHandlerUntyped;
        _createMethodDef = CreateMethodDef;
    }

    public void Intercept(IInvocation invocation)
    {
        var handler = GetHandler(invocation);
        if (handler == null)
            invocation.Proceed();
        else
            handler(invocation);
    }

    public Action<IInvocation>? GetHandler(IInvocation invocation)
        => _handlerCache.GetOrAdd(invocation.Method, _createHandlerUntyped, invocation);

    public void ValidateType(Type type)
    {
        _validateTypeCache.GetOrAdd(type, (type1, self) => {
            Log.Log(ValidationLogLevel, "Validating: '{Type}'", type1);
            try {
                self.ValidateTypeInternal(type1);
            }
            catch (Exception e) {
                Log.LogCritical(e, "Validation of '{Type}' failed", type1);
                throw;
            }
            return default;
        }, this);
    }

    protected virtual Action<IInvocation>? CreateHandlerUntyped(MethodInfo methodInfo, IInvocation initialInvocation)
    {
        var proxyMethodInfo = initialInvocation.Method;
        var methodDef = _methodDefCache.GetOrAdd(proxyMethodInfo, _createMethodDef, initialInvocation);
        if (methodDef == null)
            return null;

        return (Action<IInvocation>) CreateTypedHandlerMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .Invoke(this, new object[] { initialInvocation, methodDef })!;
    }

    // Abstract methods

    protected abstract Action<IInvocation> CreateHandler<T>(
        IInvocation initialInvocation, MethodDef methodDef);
    protected abstract MethodDef? CreateMethodDef(
        MethodInfo methodInfo, IInvocation initialInvocation);
    protected abstract void ValidateTypeInternal(Type type);
}
