using System.Net;
using System.Net.Sockets;

namespace Stl.Testing;

public static class WebTestExt
{
    public static Uri ToWss(this Uri uri)
    {
        var url = uri.ToString();
        if (url.StartsWith("http://", StringComparison.Ordinal))
            return new Uri("ws://" + url.Substring(7));
        if (url.StartsWith("https://", StringComparison.Ordinal))
            return new Uri("wss://" + url.Substring(8));
        return uri;
    }

    public static Uri GetLocalUri(int port, string protocol = "http")
        => new($"{protocol}://localhost:{port}");

    public static int GetUnusedTcpPort()
    {
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        try {
            return ((IPEndPoint) listener.LocalEndpoint).Port;
        }
        finally {
            listener.Stop();
        }
    }
}
