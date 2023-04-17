namespace Stl.Rpc.Infrastructure;

[StructLayout(LayoutKind.Auto)]
public readonly record struct RpcRequestProcessingState(
    ImmutableArray<RpcMiddleware> Middlewares,
    int NextMiddlewareIndex = 0)
{
    public bool IsFinal => NextMiddlewareIndex >= Middlewares.Length;
    public RpcMiddleware NextMiddleware => Middlewares[NextMiddlewareIndex];
    public RpcRequestProcessingState NextState => this with { NextMiddlewareIndex = NextMiddlewareIndex + 1 };

    public override string ToString()
        => $"{GetType().GetName()}({NextMiddlewareIndex}/{Middlewares.Length})";
}
