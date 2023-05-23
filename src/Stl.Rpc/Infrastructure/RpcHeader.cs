using System.Globalization;
using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

[DataContract]
public readonly record struct RpcHeader(string Name, string Value = "")
{
    public static readonly string ArgumentTypeHeaderPrefix = "@t:";
    public static readonly ImmutableArray<string> ArgumentTypeHeaders =
        Enumerable.Range(0, ArgumentList.Types.Length)
            .Select(i => ArgumentTypeHeaderPrefix + i.ToString(CultureInfo.InvariantCulture))
            .ToImmutableArray();

    private readonly string? _name = Name;
    private readonly string? _value = Value;

    [DataMember(Order = 0)]
    public string Name {
        get => _name ?? "";
        init => _name = value;
    }

    [DataMember(Order = 1)]
    public string Value {
        get => _value ?? "";
        init => _value = value;
    }

    public override string ToString()
        => $"RpcHeader({Name} = `{Value}`)";

    public bool Equals(RpcHeader other) => StringComparer.Ordinal.Equals(Name, other.Name);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
}
