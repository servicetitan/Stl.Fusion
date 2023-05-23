namespace Stl.Rpc.Infrastructure;

public static class RpcHeaderExt
{
    public static void TryAdd(this List<RpcHeader> headers, RpcHeader header)
    {
        if (!headers.TryGet(header.Name, out _))
            headers.Add(header);
    }

    public static RpcHeader? GetOrDefault(this List<RpcHeader>? headers, string name)
        => headers.TryGet(name, out var header) ? header : default;

    public static bool TryGet(this List<RpcHeader>? headers, string name, out RpcHeader header)
    {
        if (headers == null) {
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
