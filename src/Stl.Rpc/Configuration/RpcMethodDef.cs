using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcMethodDef : MethodDef
{
    private static readonly MethodInfo CreateCallFactoryMethod =
        typeof(RpcMethodDef).GetMethod(nameof(CreateCallFactory), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo InvokeCallMethod =
        typeof(RpcMethodDef).GetMethod(nameof(InvokeCall), BindingFlags.Static | BindingFlags.NonPublic)!;

    public RpcServiceDef Service { get; }
    public Symbol Name { get; }

    public Type ArgumentListType { get; }
    public Type[] RemoteParameterTypes { get; }
    public Type RemoteArgumentListType { get; }
    public bool MustCheckArguments { get; }
    public Func<ArgumentList, RpcCall> CallFactory { get; }
    public Func<RpcCall, Task> CallInvoker { get; }

    public RpcMethodDef(RpcServiceDef service, MethodInfo method, Func<RpcMethodDef, Symbol> methodNameBuilder)
        : base(service.Type, method)
    {
        ArgumentListType = ArgumentList.Types[Parameters.Length].MakeGenericType(ParameterTypes);
        if (CancellationTokenIndex >= 0) {
            var remoteParameterTypes = new Type[ParameterTypes.Length - 1];
            for (var i = 0; i < ParameterTypes.Length; i++) {
                if (i < CancellationTokenIndex)
                    remoteParameterTypes[i] = ParameterTypes[i];
                else if (i > CancellationTokenIndex)
                    remoteParameterTypes[i - 1] = ParameterTypes[i];
            }
            RemoteParameterTypes = remoteParameterTypes;
            RemoteArgumentListType = ArgumentList
                .Types[remoteParameterTypes.Length]
                .MakeGenericType(remoteParameterTypes);
        }
        else {
            RemoteParameterTypes = ParameterTypes;
            RemoteArgumentListType = ArgumentListType;
        }
        CallFactory = (Func<ArgumentList, RpcCall>)CreateCallFactoryMethod
            .MakeGenericMethod(UnwrappedReturnType)
            .Invoke(null, new object?[] { this })!;
        CallInvoker = (Func<RpcCall, Task>)InvokeCallMethod
            .MakeGenericMethod(UnwrappedReturnType)
            .CreateDelegate(typeof(Func<RpcCall, Task>));

        Service = service;
        Name = methodNameBuilder.Invoke(this);

        if (!IsAsyncMethod)
            IsValid = false;
    }

    public virtual void CheckArguments(RpcPeer peer, RpcMessage message, Type[] argumentTypes)
    { }

    // Private methods

    private static Func<ArgumentList, RpcCall> CreateCallFactory<T>(RpcMethodDef methodDef)
        => arguments => new RpcCall<T>(methodDef, arguments);

    private static Task InvokeCall<T>(RpcCall call)
    {
        var arguments = call.Arguments;
        var methodDef = call.MethodDef;
        var hub = methodDef.Service.Hub;
        var service = hub.Services.GetRequiredService(methodDef.Service.ServerType);
        var result = arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        if (methodDef.ReturnsTask) {
            var task = (Task)result!;
            if (methodDef.IsAsyncVoidMethod)
                task = ToUnitTask(task);
            return task;
        }

        if (methodDef.ReturnsValueTask) {
            if (result is ValueTask<T> valueTask)
                return valueTask.AsTask();
            if (result is ValueTask voidValueTask)
                return ToUnitTask(voidValueTask);
        }

        return Task.FromResult((T)result!);
    }

    private static async Task<Unit> ToUnitTask(Task source)
    {
        await source.ConfigureAwait(false);
        return default;
    }

    private static async Task<Unit> ToUnitTask(ValueTask source)
    {
        await source.ConfigureAwait(false);
        return default;
    }
}
