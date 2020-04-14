using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Stl.Purifier.Autofac
{
    public class ExtendedMethodInfo
    {
        public MethodInfo Method { get; private set; } = null!;
        public Type OutputType { get; private set; } = null!;
        public bool ReturnsValueTask { get; private set; }
        public bool ReturnsComputed { get; private set; }
        public int CancellationTokenArgumentIndex { get; private set; } = -1;
        public int ProceedInfoArgumentIndex { get; private set; } = -1;
        public int UsedArgumentBitmap { get; private set; } = int.MaxValue;

        private ExtendedMethodInfo() {}

        public static ExtendedMethodInfo? Create(MethodInfo method)
        {
            var returnType = method.ReturnType;
            if (!returnType.IsGenericType)
                return null;

            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            var returnsTask = returnTypeGtd == typeof(Task<>);
            var returnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            if (!(returnsTask || returnsValueTask))
                return null;

            var outputType = returnType.GetGenericArguments()[0];
            var returnsComputed = false;
            if (outputType.IsGenericType) {
                var returnTypeArgGtd = outputType.GetGenericTypeDefinition();
                if (returnTypeArgGtd == typeof(IComputed<>)) {
                    returnsComputed = true;
                    outputType = outputType.GetGenericArguments()[0];
                }
            }

            var r = new ExtendedMethodInfo {
                Method = method,
                OutputType = outputType,
                ReturnsValueTask = returnsValueTask,
                ReturnsComputed = returnsComputed,
            };
            var index = 0;
            foreach (var p in method.GetParameters()) {
                if (typeof(IInvocationProceedInfo).IsAssignableFrom(p.ParameterType))
                    r.ProceedInfoArgumentIndex = index;
                if (typeof(CancellationToken).IsAssignableFrom(p.ParameterType))
                    r.CancellationTokenArgumentIndex = index;
                index++;
            }
            if (r.ProceedInfoArgumentIndex >= 0)
                r.UsedArgumentBitmap ^= 1 << r.ProceedInfoArgumentIndex;
            if (r.CancellationTokenArgumentIndex >= 0)
                r.UsedArgumentBitmap ^= 1 << r.CancellationTokenArgumentIndex;

            return r.ProceedInfoArgumentIndex < 0 ? null : r;
        }
    }
}
