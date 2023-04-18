using System.Globalization;
using Cysharp.Text;
using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public readonly record struct RpcHeader(
    string Name,
    string Value = "")
{
    public static readonly string ArgumentTypeHeaderPrefix = "@t:";
    public static readonly ImmutableArray<string> ArgumentTypeHeaders =
        Enumerable.Range(0, ArgumentList.Types.Length)
            .Select(i => ArgumentTypeHeaderPrefix + i.ToString(CultureInfo.InvariantCulture))
            .ToImmutableArray();

    public override string ToString()
        => ZString.Concat(Name, ": ", Value);

    public bool Equals(RpcHeader other) => StringComparer.Ordinal.Equals(Name, other.Name);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
}
