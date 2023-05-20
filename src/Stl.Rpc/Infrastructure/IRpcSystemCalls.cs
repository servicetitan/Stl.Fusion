namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Ok(object? result);
    Task<RpcNoWait> Fail(ExceptionInfo error);
    Task<RpcNoWait> Cancel();
}

public interface IRpcSystemCallsClient : IRpcSystemCalls, IRpcClient
{ }
