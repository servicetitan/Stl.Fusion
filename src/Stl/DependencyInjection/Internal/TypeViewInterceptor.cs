using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Stl.Async;
using Stl.Concurrency;
using Stl.Extensibility;

namespace Stl.DependencyInjection.Internal
{
    public class TypeViewInterceptor : IInterceptor
    {
        public static TypeViewInterceptor Default { get; } = new TypeViewInterceptor();

        private readonly Func<(MethodInfo, Type), IInvocation, Action<IInvocation>?> _createHandler;
        private readonly ConcurrentDictionary<(MethodInfo, Type), Action<IInvocation>?> _handlerCache =
            new ConcurrentDictionary<(MethodInfo, Type), Action<IInvocation>?>();
        private readonly MethodInfo _createConvertingHandlerMethod;
        private readonly MethodInfo _createTaskConvertingHandlerMethod;
        private readonly MethodInfo _createValueTaskConvertingHandlerMethod;

        public TypeViewInterceptor()
        {
            _createHandler = CreateHandler;
            _createConvertingHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateConvertingHandler));
            _createTaskConvertingHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateTaskConvertingHandler));
            _createValueTaskConvertingHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateValueTaskConvertingHandler));
        }

        public void Intercept(IInvocation invocation)
        {
            var key = (invocation.Method, invocation.Proxy.GetType());
            var handler = _handlerCache.GetOrAddChecked(key, _createHandler, invocation);
            if (handler == null)
                invocation.Proceed();
            else
                handler.Invoke(invocation);
        }

        protected virtual Action<IInvocation>? CreateHandler((MethodInfo, Type) key, IInvocation initialInvocation)
        {
            var (methodInfo, tProxy) = key;
            var tTarget = initialInvocation.TargetType;
            var mSource = initialInvocation.Method;
            var mArgTypes = mSource.GetParameters().Select(p => p.ParameterType).ToArray();
            var mTarget = tTarget.GetMethod(mSource.Name, mArgTypes);
            var fTarget = tProxy.GetField("__target", BindingFlags.Instance | BindingFlags.NonPublic);

            Type? TryGetTaskOfTArgument(Type t) {
                if (!t.IsGenericType)
                    return null;
                var tg = t.GetGenericTypeDefinition();
                if (tg != typeof(Task<>))
                    return null;
                return t.GetGenericArguments()[0];
            }

            Type? TryGetValueTaskOfTArgument(Type t) {
                if (!t.IsGenericType)
                    return null;
                var tg = t.GetGenericTypeDefinition();
                if (tg != typeof(ValueTask<>))
                    return null;
                return t.GetGenericArguments()[0];
            }

            if (mTarget.ReturnType != mSource.ReturnType) {
                Action<IInvocation>? result;

                // Trying Task<T>
                var rtSource = TryGetTaskOfTArgument(mSource.ReturnType);
                var rtTarget = TryGetTaskOfTArgument(mTarget.ReturnType);
                if (rtSource != null && rtTarget != null) {
                    result = (Action<IInvocation>?) _createTaskConvertingHandlerMethod
                        .MakeGenericMethod(rtSource, rtTarget)
                        .Invoke(this, new object[] {initialInvocation, fTarget, mTarget});
                    if (result != null)
                        return result;
                }

                // Trying ValueTask<T>
                rtSource = TryGetValueTaskOfTArgument(mSource.ReturnType);
                rtTarget = TryGetValueTaskOfTArgument(mTarget.ReturnType);
                if (rtSource != null && rtTarget != null) {
                    result = (Action<IInvocation>?) _createValueTaskConvertingHandlerMethod
                        .MakeGenericMethod(rtSource, rtTarget)
                        .Invoke(this, new object[] {initialInvocation, fTarget, mTarget});
                    if (result != null)
                        return result;
                }

                // The only option is to convert types directly
                rtSource = mSource.ReturnType;
                rtTarget = mTarget.ReturnType;
                result = (Action<IInvocation>?) _createConvertingHandlerMethod
                    .MakeGenericMethod(rtSource, rtTarget)
                    .Invoke(this, new object[] {initialInvocation, fTarget, mTarget});
                if (result != null)
                    return result;
            }

            return invocation => {
                // TODO: Get rid of reflection here (not critical)
                var target = fTarget.GetValue(invocation.Proxy);
                invocation.ReturnValue = mTarget.Invoke(target, invocation.Arguments);
            };
        }

        protected virtual Action<IInvocation>? CreateConvertingHandler<TSource, TTarget>(
            IInvocation initialInvocation, FieldInfo fTarget, MethodInfo mTarget)
        {
            var tSource = typeof(TSource);
            var tTarget = typeof(TTarget);

            // Fast conversion via IConvertibleTo<T>
            if (typeof(IConvertibleTo<>).MakeGenericType(tSource).IsAssignableFrom(tTarget)) {
                return invocation => {
                    var target = fTarget.GetValue(invocation.Proxy);
                    var result = (TTarget) mTarget.Invoke(target, invocation.Arguments);
                    invocation.ReturnValue = result is IConvertibleTo<TSource> c ? c.Convert() : default!;
                };
            }

            // Slow conversion via TypeConverter(s)
            var d = TypeDescriptor.GetConverter(tTarget);
            if (!d.CanConvertTo(tSource))
                return null;

            return invocation => {
                // TODO: Get rid of reflection here (not critical)
                var target = fTarget.GetValue(invocation.Proxy);
                var result = (TTarget) mTarget.Invoke(target, invocation.Arguments);
                invocation.ReturnValue = (TSource) d.ConvertTo(result, tSource)!;
            };
        }

        protected virtual Action<IInvocation>? CreateTaskConvertingHandler<TSource, TTarget>(
            IInvocation initialInvocation, FieldInfo fTarget, MethodInfo mTarget)
        {
            var tSource = typeof(TSource);
            var tTarget = typeof(TTarget);

            // Fast conversion via IConvertibleTo<T>
            if (typeof(IConvertibleTo<>).MakeGenericType(tSource).IsAssignableFrom(tTarget)) {
                return invocation => {
                    var target = fTarget.GetValue(invocation.Proxy);
                    var untypedResult = mTarget.Invoke(target, invocation.Arguments);
                    var result = (Task<TTarget>) untypedResult;
                    invocation.ReturnValue = result.ContinueWith(t =>
                        t.Result is IConvertibleTo<TSource> c ? c.Convert() : default!);
                };
            }

            // Slow conversion via TypeConverter(s)
            var d = TypeDescriptor.GetConverter(tTarget);
            if (!d.CanConvertTo(tSource))
                return null;

            return invocation => {
                // TODO: Get rid of reflection here (not critical)
                var target = fTarget.GetValue(invocation.Proxy);
                var untypedResult = mTarget.Invoke(target, invocation.Arguments);
                var result = (Task<TTarget>) untypedResult;
                invocation.ReturnValue = result.ContinueWith(t =>
                    (TSource) d.ConvertTo(t.Result!, tSource)!);
            };
        }

        protected virtual Action<IInvocation>? CreateValueTaskConvertingHandler<TSource, TTarget>(
            IInvocation initialInvocation, FieldInfo fTarget, MethodInfo mTarget)
        {
            var tSource = typeof(TSource);
            var tTarget = typeof(TTarget);

            // Fast conversion via IConvertibleTo<T>
            if (typeof(IConvertibleTo<>).MakeGenericType(tSource).IsAssignableFrom(tTarget)) {
                return invocation => {
                    var target = fTarget.GetValue(invocation.Proxy);
                    var untypedResult = mTarget.Invoke(target, invocation.Arguments);
                    var result = (ValueTask<TTarget>) untypedResult;
                    // ReSharper disable once HeapView.BoxingAllocation
                    invocation.ReturnValue = result.AsTask().ContinueWith(t =>
                        t.Result is IConvertibleTo<TSource> c ? c.Convert() : default!).ToValueTask();
                };
            }

            // Slow conversion via TypeConverter(s)
            var d = TypeDescriptor.GetConverter(tTarget);
            if (!d.CanConvertTo(tSource))
                return null;

            return invocation => {
                // TODO: Get rid of reflection here (not critical)
                var target = fTarget.GetValue(invocation.Proxy);
                var untypedResult = mTarget.Invoke(target, invocation.Arguments);
                var result = (ValueTask<TTarget>) untypedResult;
                // ReSharper disable once HeapView.BoxingAllocation
                invocation.ReturnValue = result.AsTask().ContinueWith(t =>
                    (TSource) d.ConvertTo(t.Result!, tSource)!).ToValueTask();
            };
        }
    }
}
