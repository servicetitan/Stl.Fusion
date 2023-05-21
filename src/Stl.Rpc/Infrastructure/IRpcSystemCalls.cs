namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService, IRpcClient
{
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
}
