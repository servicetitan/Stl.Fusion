namespace Stl.Rpc.Infrastructure;

public class RpcSystemCallService : RpcServiceBase, IRpcSystemService
{
    public static readonly Symbol Name = "$sys";

    public RpcSystemCallService(IServiceProvider services) : base(services)
    { }
}
