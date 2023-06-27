using Microsoft.Toolkit.HighPerformance;

namespace Stl.Rpc.Caching;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class RpcCacheKey : IEquatable<RpcCacheKey>
{
    private readonly int _hashCode;

    [DataMember(Order = 0), MemoryPackOrder(0)] public readonly Symbol Service;
    [DataMember(Order = 1), MemoryPackOrder(1)] public readonly Symbol Method;
    [DataMember(Order = 2), MemoryPackOrder(2)] public readonly TextOrBytes ArgumentData;

    [MemoryPackConstructor]
    public RpcCacheKey(Symbol service, Symbol method, TextOrBytes argumentData)
    {
        Service = service;
        Method = method;
        ArgumentData = argumentData;
        _hashCode = unchecked(
            Service.Value.GetDjb2HashCode()
            ^ (353*Method.Value.GetDjb2HashCode())
            ^ argumentData.GetDataHashCode());
    }

    public override string ToString()
        => $"#{_hashCode}: {Service}.{Method}({Convert.ToBase64String(ArgumentData.Bytes)})";

    // Equality

    public bool Equals(RpcCacheKey? other)
        =>  !ReferenceEquals(other, null)
            && _hashCode == other._hashCode
            && StringComparer.Ordinal.Equals(Method.Value, other.Method.Value)
            && StringComparer.Ordinal.Equals(Service.Value, other.Service.Value)
            && ArgumentData.DataEquals(other.ArgumentData);

    public override bool Equals(object? obj) => obj is RpcCacheKey other && Equals(other);
    public override int GetHashCode() => _hashCode;
    public static bool operator ==(RpcCacheKey left, RpcCacheKey right) => left.Equals(right);
    public static bool operator !=(RpcCacheKey left, RpcCacheKey right) => !left.Equals(right);
}
