namespace Stl.Interception.Interceptors;

public abstract class InterceptorBase : IHasServices
{
    public record Options
    {
        public bool IsLoggingEnabled { get; init; } = true;
    }

    private static readonly MethodInfo CreateTypedHandlerMethod = typeof(InterceptorBase)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateHandler)));

    private readonly Func<MethodInfo, Invocation, Action<Invocation>?> _createHandlerUntyped;
    private readonly Func<MethodInfo, Invocation, MethodDef?> _createMethodDef;
    private readonly ConcurrentDictionary<MethodInfo, MethodDef?> _methodDefCache = new();
    private readonly ConcurrentDictionary<MethodInfo, Action<Invocation>?> _handlerCache = new();
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

    public void Intercept(Invocation invocation)
    {
        var handler = GetHandler(invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    public Action<Invocation>? GetHandler(Invocation invocation)
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

    protected virtual Action<Invocation>? CreateHandlerUntyped(MethodInfo methodInfo, Invocation initialInvocation)
    {
        var proxyMethodInfo = initialInvocation.Method;
        var methodDef = _methodDefCache.GetOrAdd(proxyMethodInfo, _createMethodDef, initialInvocation);
        if (methodDef == null)
            return null;

        return (Action<Invocation>) CreateTypedHandlerMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .Invoke(this, new object[] { initialInvocation, methodDef })!;
    }

    // Abstract methods

    protected abstract Action<Invocation> CreateHandler<T>(
        Invocation initialInvocation, MethodDef methodDef);
    protected abstract MethodDef? CreateMethodDef(
        MethodInfo methodInfo, Invocation initialInvocation);
    protected abstract void ValidateTypeInternal(Type type);
}
