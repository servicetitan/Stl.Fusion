using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc.Diagnostics;

namespace Stl.Rpc;

public sealed class RpcMethodDef : MethodDef
{
    private string? _toStringCached;

    public RpcHub Hub { get; }
    public RpcServiceDef Service { get; }
    public Symbol Name { get; }
    public new Symbol FullName { get; }

    public Type ArgumentListType { get; }
    public bool HasObjectTypedArguments { get; }
    public bool AllowArgumentPolymorphism { get; }
    public bool AllowResultPolymorphism { get; }
    public Func<ArgumentList> ArgumentListFactory { get; }
    public Func<ArgumentList> ResultListFactory { get; }
    public bool NoWait { get; }
    public RpcMethodTracer? Tracer { get; }

    public RpcMethodDef(
        RpcServiceDef service,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        MethodInfo method)
        : base(serviceType, method)
    {
        if (serviceType != service.Type)
            throw new ArgumentOutOfRangeException(nameof(serviceType));

        Hub = service.Hub;
        ArgumentListType = Parameters.Length == 0
            ? ArgumentList.Types[0]
            : ArgumentList.Types[Parameters.Length].MakeGenericType(ParameterTypes);
        HasObjectTypedArguments = ParameterTypes.Any(type => typeof(object) == type);
        NoWait = UnwrappedReturnType == typeof(RpcNoWait);

        Service = service;
        Name = Hub.MethodNameBuilder.Invoke(this);
        FullName = $"{service.Name.Value}.{Name.Value}";
        AllowResultPolymorphism = AllowArgumentPolymorphism = service.IsSystem || service.IsBackend;

#pragma warning disable IL2055, IL2072
        ArgumentListFactory = (Func<ArgumentList>)ArgumentListType.GetConstructorDelegate()!;
        ResultListFactory = (Func<ArgumentList>)ArgumentList.Types[1]
            .MakeGenericType(UnwrappedReturnType)
            .GetConstructorDelegate()!;
#pragma warning restore IL2055, IL2072

        if (!IsAsyncMethod)
            IsValid = false;

        Tracer = Hub.MethodTracerFactory.Invoke(this);
    }

    public override string ToString()
    {
        if (_toStringCached != null)
            return _toStringCached;

        var arguments = ParameterTypes.Select(t => t.GetName()).ToDelimitedString();
        var returnType = UnwrappedReturnType.GetName();
        return _toStringCached = $"'{Name}': ({arguments}) -> {returnType}{(IsValid ? "" : " - invalid")}";
    }
}
