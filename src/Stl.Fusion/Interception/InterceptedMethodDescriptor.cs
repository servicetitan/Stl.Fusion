using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public class InterceptedMethodDescriptor
    {
        public bool IsValid => MethodInfo != null;
        public MethodInfo MethodInfo { get; } = null!;
        public Type OutputType { get; } = null!;
        public bool ReturnsValueTask { get; }
        public ComputedOptions Options { get; set; } = ComputedOptions.Default;

        public InterceptorBase Interceptor { get; } = null!;
        public ArgumentHandler InvocationTargetHandler { get; } = null!;
        public ArgumentHandler[] ArgumentHandlers { get; } = null!;
        public (ArgumentHandler Handler, int Index)[]? PreprocessingArgumentHandlers { get; }
        public int CancellationTokenArgumentIndex { get; } = -1;

        public InterceptedMethodDescriptor(
            InterceptorBase interceptor,
            MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return;

            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            var returnsTask = returnTypeGtd == typeof(Task<>);
            var returnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            if (!(returnsTask || returnsValueTask))
                return;
            var outputType = returnType.GetGenericArguments()[0];
            var invocationTargetType = methodInfo.ReflectedType;
            var parameters = methodInfo.GetParameters();

            var options = interceptor.ComputedOptionsProvider.GetComputedOptions(interceptor, methodInfo);
            if (options == null)
                return;

            Interceptor = interceptor;
            MethodInfo = methodInfo;
            OutputType = outputType;
            ReturnsValueTask = returnsValueTask;
            Options = options;

            var argumentHandlerProvider = Interceptor.ArgumentHandlerProvider;
            InvocationTargetHandler = argumentHandlerProvider.GetInvocationTargetHandler(methodInfo, invocationTargetType!);
            ArgumentHandlers = new ArgumentHandler[parameters.Length];
            var preprocessingArgumentHandlers = new List<(ArgumentHandler Handler, int Index)>();
            for (var i = 0; i < parameters.Length; i++) {
                var p = parameters[i];
                var argumentHandler = argumentHandlerProvider.GetArgumentHandler(methodInfo, p);
                ArgumentHandlers[i] = argumentHandler;
                if (argumentHandler.PreprocessFunc != null)
                    preprocessingArgumentHandlers.Add((argumentHandler, i));
                var parameterType = p.ParameterType;
                if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                    CancellationTokenArgumentIndex = i;
            }
            if (preprocessingArgumentHandlers.Count != 0)
                PreprocessingArgumentHandlers = preprocessingArgumentHandlers.ToArray();
        }

        public virtual InterceptedInput CreateInput(IFunction function, IInvocation invocation)
            => new InterceptedInput(function, this, invocation);
    }
}
