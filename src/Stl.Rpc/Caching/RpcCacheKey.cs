using System.Buffers.Text;

namespace Stl.Rpc.Caching;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcCacheKey : IEquatable<RpcCacheKey>
{
    private readonly int _argumentDataHashCode;

    [DataMember(Order = 0), MemoryPackOrder(0)] public readonly Symbol Service;
    [DataMember(Order = 1), MemoryPackOrder(1)] public readonly Symbol Method;
    [DataMember(Order = 2), MemoryPackOrder(2)] public readonly TextOrBytes ArgumentData;

    [MemoryPackConstructor]
    public RpcCacheKey(Symbol service, Symbol method, TextOrBytes argumentData)
    {
        Service = service;
        Method = method;
        ArgumentData = argumentData;
        _argumentDataHashCode = argumentData.GetDataHashCode();
    }

    public override string ToString()
        => $"#{GetHashCode()}: {Service}.{Method}({Convert.ToBase64String(ArgumentData.Bytes)})";

    // Equality

    public bool Equals(RpcCacheKey? other)
        =>  !ReferenceEquals(other, null)
            && _argumentDataHashCode == other._argumentDataHashCode
            && Method.Equals(other.Method)
            && Service.Equals(other.Service)
            && ArgumentData.DataEquals(other.ArgumentData);

    public override bool Equals(object? obj) => obj is RpcCacheKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Service, Method, _argumentDataHashCode);
    public static bool operator ==(RpcCacheKey left, RpcCacheKey right) => left.Equals(right);
    public static bool operator !=(RpcCacheKey left, RpcCacheKey right) => !left.Equals(right);
}
