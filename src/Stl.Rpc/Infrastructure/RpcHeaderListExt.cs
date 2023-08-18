namespace Stl.Rpc.Infrastructure;

public static class RpcHeaderListExt
{
    private static readonly List<RpcHeader> Empty = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<RpcHeader> OrNew(this List<RpcHeader>? headers)
        => headers ?? new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<RpcHeader> OrEmpty(this List<RpcHeader>? headers)
        => headers ?? Empty;

    public static List<RpcHeader> TryAdd(this List<RpcHeader>? headers, RpcHeader header)
    {
        if (headers.TryGet(header.Name, out _))
            return headers!;

        headers ??= new();
        headers.Add(header);
        return headers;
    }

    public static RpcHeader? GetOrDefault(this List<RpcHeader>? headers, string name)
        => headers.TryGet(name, out var header) ? header : default;

    public static bool TryGet(this List<RpcHeader>? headers, string name, out RpcHeader header)
    {
        if (headers == null || headers.Count == 0) {
            header = default;
            return false;
        }

        foreach (var h in headers)
            if (StringComparer.Ordinal.Equals(h.Name, name)) {
                header = h;
                return true;
            }

        header = default;
        return false;
    }
}
