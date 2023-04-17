namespace Stl.Rpc.Infrastructure;

public static class RpcHeaderExt
{
    public static bool TryGet(this RpcHeader[]? headers, string name, out RpcHeader header)
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
