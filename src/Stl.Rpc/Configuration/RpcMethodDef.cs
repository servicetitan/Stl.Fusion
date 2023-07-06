using Stl.Interception;
using Stl.Interception.Interceptors;

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

    public RpcMethodDef(RpcServiceDef service, MethodInfo method)
        : base(service.Type, method)
    {
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

        ArgumentListFactory = (Func<ArgumentList>)ArgumentListType
            .GetConstructorDelegate()!;
        ResultListFactory = (Func<ArgumentList>)ArgumentList.Types[1]
            .MakeGenericType(UnwrappedReturnType)
            .GetConstructorDelegate()!;

        if (!IsAsyncMethod)
            IsValid = false;
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
