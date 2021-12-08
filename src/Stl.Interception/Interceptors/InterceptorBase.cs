using Castle.DynamicProxy;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Concurrency;

namespace Stl.Interception.Interceptors;

public abstract class InterceptorBase : IOptionalInterceptor, IHasServices
{
    public class Options
    {
        public bool IsLoggingEnabled { get; set; } = true;
    }

    private readonly MethodInfo _createTypedHandlerMethod;
    private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandlerUntyped;
    private readonly Func<MethodInfo, IInvocation, MethodDef?> _createInterceptedMethod;
    private readonly ConcurrentDictionary<MethodInfo, MethodDef?> _interceptedMethodCache = new();
    private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache = new();
    private readonly ConcurrentDictionary<Type, Unit> _validateTypeCache = new();

    protected ILoggerFactory LoggerFactory { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Debug;
    protected LogLevel ValidationLogLevel { get; set; } = LogLevel.Information;

    public IServiceProvider Services { get; }

    protected InterceptorBase(
        Options options,
        IServiceProvider services,
        ILoggerFactory? loggerFactory = null)
    {
        LoggerFactory = loggerFactory ??= NullLoggerFactory.Instance;
        Log = LoggerFactory.CreateLogger(GetType());
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);

        Services = services;
        _createHandlerUntyped = CreateHandlerUntyped;
        _createInterceptedMethod = CreateMethodDef;
        _createTypedHandlerMethod = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateHandler)));
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
        => _handlerCache.GetOrAddChecked(invocation.Method, _createHandlerUntyped, invocation);

    public void ValidateType(Type type)
    {
        _validateTypeCache.GetOrAddChecked(type, (type1, self) => {
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
        var method = _interceptedMethodCache.GetOrAddChecked(proxyMethodInfo, _createInterceptedMethod, initialInvocation);
        if (method == null)
            return null;

        return (Action<IInvocation>) _createTypedHandlerMethod
            .MakeGenericMethod(method.UnwrappedReturnType)
            .Invoke(this, new object[] {initialInvocation, method})!;
    }

    // Abstract methods

    protected abstract Action<IInvocation> CreateHandler<T>(
        IInvocation initialInvocation, MethodDef methodDef);
    protected abstract MethodDef? CreateMethodDef(
        MethodInfo methodInfo, IInvocation initialInvocation);
    protected abstract void ValidateTypeInternal(Type type);
}
