namespace Stl.Rpc.Infrastructure;

public interface IRpcSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Result(object? result);
    Task<RpcNoWait> Error(ExceptionInfo error);
}

public interface IRpcSystemCallsClient : IRpcSystemCalls, IRpcClient
{ }
