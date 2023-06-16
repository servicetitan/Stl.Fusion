using System.Globalization;
using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public static class RpcSystemHeaders
{
    public static readonly string ArgumentTypeHeaderPrefix = "@t:";
    public static readonly ImmutableArray<RpcHeader> ArgumentTypes =
        Enumerable.Range(0, ArgumentList.Types.Length)
            .Select(i => new RpcHeader(ArgumentTypeHeaderPrefix + i.ToString(CultureInfo.InvariantCulture)))
            .ToImmutableArray();
}

[DataContract]
public readonly record struct RpcHeader(string Name, string Value = "")
{
    private readonly string? _name = Name;
    private readonly string? _value = Value;

    [DataMember(Order = 0)]
    public string Name {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _name ?? "";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        init => _name = value;
    }

    [DataMember(Order = 1)]
    public string Value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value ?? "";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        init => _value = value;
    }

    public override string ToString()
        => $"(`{Name}`, `{Value}`)";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RpcHeader With(string value)
        => new(Name, value);

    // Equality is based solely on header name
    public bool Equals(RpcHeader other) => StringComparer.Ordinal.Equals(Name, other.Name);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
}
