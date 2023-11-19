using System.Diagnostics.CodeAnalysis;

namespace Stl.Interception.Interceptors;

public abstract class InterceptorBase : Interceptor, IHasServices
{
    public record Options
    {
        public static class Defaults
        {
            public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
            public static LogLevel ValidationLogLevel { get; set; } = LogLevel.Debug;
            public static bool IsValidationEnabled { get; set; } = true;
        }

        public LogLevel LogLevel { get; set; } = Defaults.LogLevel;
        public LogLevel ValidationLogLevel { get; set; } = Defaults.ValidationLogLevel;
        public bool IsValidationEnabled { get; init; } = Defaults.IsValidationEnabled;
    }

    private static readonly MethodInfo CreateTypedHandlerMethod = typeof(InterceptorBase)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateHandler)));

    private readonly Func<MethodInfo, Invocation, Func<Invocation, object?>?> _createHandlerUntyped;
    private readonly Func<MethodInfo, Invocation, MethodDef?> _createMethodDef;
    private readonly ConcurrentDictionary<MethodInfo, MethodDef?> _methodDefCache = new();
    private readonly ConcurrentDictionary<MethodInfo, Func<Invocation, object?>?> _handlerCache = new(1, 64);
    private readonly ConcurrentDictionary<Type, Unit> _validateTypeCache = new();

    protected readonly ILogger Log;
    protected readonly ILogger? DefaultLog;
    protected readonly ILogger? ValidationLog;
    protected readonly LogLevel LogLevel;
    protected readonly LogLevel ValidationLogLevel;

    public bool IsValidationEnabled { get; }
    public IServiceProvider Services { get; }

    protected InterceptorBase(Options settings, IServiceProvider services)
    {
        Services = services;
        LogLevel = settings.LogLevel;
        ValidationLogLevel = settings.ValidationLogLevel;
        IsValidationEnabled = settings.IsValidationEnabled;

        Log = Services.LogFor(GetType());
        DefaultLog = Log.IfEnabled(settings.LogLevel);
        ValidationLog = Log.IfEnabled(settings.ValidationLogLevel);

        _createHandlerUntyped = CreateHandlerUntyped;
        _createMethodDef = CreateMethodDef;
    }

    public override void Intercept(Invocation invocation)
    {
        var handler = GetHandler(invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        var handler = GetHandler(invocation);
        return handler == null
            ? invocation.Intercepted<TResult>()
            : (TResult)handler.Invoke(invocation)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<Invocation, object?>? GetHandler(Invocation invocation)
        => _handlerCache.GetOrAdd(invocation.Method, _createHandlerUntyped, invocation);

    public void ValidateType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (!IsValidationEnabled)
            return;

        _validateTypeCache.GetOrAdd(type, static (type1, self) => {
            self.ValidationLog?.Log(self.ValidationLogLevel, "Validating: '{Type}'", type1);
            try {
#pragma warning disable IL2067
                self.ValidateTypeInternal(type1);
#pragma warning restore IL2067
            }
            catch (Exception e) {
                self.Log.LogCritical(e, "Validation of '{Type}' failed", type1);
                throw;
            }
            return default;
        }, this);
    }

    protected virtual Func<Invocation, object?>? CreateHandlerUntyped(MethodInfo method, Invocation initialInvocation)
    {
        var proxyMethodInfo = initialInvocation.Method;
        var methodDef = _methodDefCache.GetOrAdd(proxyMethodInfo, _createMethodDef, initialInvocation);
        if (methodDef == null)
            return null;

        return (Func<Invocation, object?>)CreateTypedHandlerMethod
            .MakeGenericMethod(methodDef.UnwrappedReturnType)
            .Invoke(this, new object[] { initialInvocation, methodDef })!;
    }

    // Abstract methods

    protected abstract Func<Invocation, object?> CreateHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Invocation initialInvocation, MethodDef methodDef);
    // We don't need to decorate this method with any dynamic access attributes
    protected abstract MethodDef? CreateMethodDef(
        MethodInfo method, Invocation initialInvocation);
    protected abstract void ValidateTypeInternal(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type);
}
