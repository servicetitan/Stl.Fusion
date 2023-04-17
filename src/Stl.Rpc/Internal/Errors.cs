namespace Stl.Rpc.Internal;

public static class Errors
{
    public static Exception NoMoreMiddlewares()
        => new InvalidOperationException("The very last RpcMiddleware tries to invoke the next one.");
}
