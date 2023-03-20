using Stl.Conversion;

namespace Stl.Interception.Interceptors;

public class TypeViewInterceptor : Interceptor
{
    private readonly Func<(MethodInfo, Type), Invocation, Delegate?> _createHandler;
    private readonly ConcurrentDictionary<(MethodInfo, Type), Delegate?> _handlerCache = new();
    private readonly MethodInfo _createConvertingHandlerMethod;
    private readonly MethodInfo _createTaskConvertingHandlerMethod;
    private readonly MethodInfo _createValueTaskConvertingHandlerMethod;

    protected IServiceProvider Services { get; }

    public TypeViewInterceptor(IServiceProvider services)
    {
        Services = services;
        _createHandler = CreateHandler;
        _createConvertingHandlerMethod = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateConvertingHandler)));
        _createTaskConvertingHandlerMethod = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateTaskConvertingHandler)));
        _createValueTaskConvertingHandlerMethod = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(CreateValueTaskConvertingHandler)));
    }

    public override void Intercept(Invocation invocation)
    {
        var key = (invocation.Method, invocation.Proxy.GetType());
        var handler = _handlerCache.GetOrAdd(key, _createHandler, invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    protected virtual Delegate? CreateHandler((MethodInfo, Type) key, Invocation initialInvocation)
    {
        var (_, tProxy) = key;
        var view = (TypeView) initialInvocation.Proxy;
        var tTarget = view.ViewTarget.GetType();
        var mSource = initialInvocation.Method;
        var mArgTypes = mSource.GetParameters().Select(p => p.ParameterType).ToArray();
        var mTarget = tTarget.GetMethod(mSource.Name, mArgTypes);

        Type? GetTaskOfTArgument(Type t) {
            if (!t.IsGenericType)
                return null;
            var tg = t.GetGenericTypeDefinition();
            if (tg != typeof(Task<>))
                return null;
            return t.GetGenericArguments()[0];
        }

        Type? GetValueTaskOfTArgument(Type t) {
            if (!t.IsGenericType)
                return null;
            var tg = t.GetGenericTypeDefinition();
            if (tg != typeof(ValueTask<>))
                return null;
            return t.GetGenericArguments()[0];
        }

        if (mTarget!.ReturnType != mSource.ReturnType) {
            Action<Invocation>? result;

            // Trying Task<T>
            var rtSource = GetTaskOfTArgument(mSource.ReturnType);
            var rtTarget = GetTaskOfTArgument(mTarget.ReturnType);
            if (rtSource != null && rtTarget != null) {
                result = (Action<Invocation>?) _createTaskConvertingHandlerMethod
                    .MakeGenericMethod(rtSource, rtTarget)
                    .Invoke(this, new object[] {initialInvocation, mTarget});
                if (result != null)
                    return result;
            }

            // Trying ValueTask<T>
            rtSource = GetValueTaskOfTArgument(mSource.ReturnType);
            rtTarget = GetValueTaskOfTArgument(mTarget.ReturnType);
            if (rtSource != null && rtTarget != null) {
                result = (Action<Invocation>?) _createValueTaskConvertingHandlerMethod
                    .MakeGenericMethod(rtSource, rtTarget)
                    .Invoke(this, new object[] {initialInvocation, mTarget});
                if (result != null)
                    return result;
            }

            // The only option is to convert types directly
            rtSource = mSource.ReturnType;
            rtTarget = mTarget.ReturnType;
            result = (Action<Invocation>?) _createConvertingHandlerMethod
                .MakeGenericMethod(rtSource, rtTarget)
                .Invoke(this, new object[] {initialInvocation, mTarget});
            if (result != null)
                return result;
        }

        return invocation => {
            // TODO: Get rid of reflection here (not critical)
            var target = ((TypeView) invocation.Proxy).ViewTarget;
            invocation.ReturnValue = mTarget.Invoke(target, invocation.Arguments);
        };
    }

    protected virtual Action<IInvocation>? CreateConvertingHandler<TSource, TTarget>(
        IInvocation initialInvocation, MethodInfo mTarget)
    {
        // !!! Note that TSource is type to convert to here, and TTarget is type to convert from
        var converter = Services.Converters().From<TTarget>().To<TSource>();
        if (!converter.IsAvailable)
            return null;

        return invocation => {
            var target = ((TypeView) invocation.Proxy).ViewTarget;
            var result = (TTarget) mTarget.Invoke(target, invocation.Arguments)!;
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            invocation.ReturnValue = converter.Convert(result);
        };
    }

    protected virtual Action<IInvocation>? CreateTaskConvertingHandler<TSource, TTarget>(
        IInvocation initialInvocation, MethodInfo mTarget)
    {
        // !!! Note that TSource is type to convert to here, and TTarget is type to convert from
        var converter = Services.Converters().From<TTarget>().To<TSource>();
        if (!converter.IsAvailable)
            return null;

        return invocation => {
            var target = ((TypeView) invocation.Proxy).ViewTarget;
            var untypedResult = mTarget.Invoke(target, invocation.Arguments);
            var result = (Task<TTarget>) untypedResult!;
            invocation.ReturnValue = result.ContinueWith(
                t => converter.Convert(t.Result),
                default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        };
    }

    protected virtual Action<IInvocation>? CreateValueTaskConvertingHandler<TSource, TTarget>(
        IInvocation initialInvocation, MethodInfo mTarget)
    {
        // !!! Note that TSource is type to convert to here, and TTarget is type to convert from
        var converter = Services.Converters().From<TTarget>().To<TSource>();
        if (!converter.IsAvailable)
            return null;

        return invocation => {
            var target = ((TypeView) invocation.Proxy).ViewTarget;
            var untypedResult = mTarget.Invoke(target, invocation.Arguments);
            var result = (ValueTask<TTarget>) untypedResult!;
            // ReSharper disable once HeapView.BoxingAllocation
            invocation.ReturnValue = result
                .AsTask()
                .ContinueWith(
                    t => converter.Convert(t.Result),
                    default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .ToValueTask();
        };
    }
}
