using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcErrorClassifier
{
    public virtual bool IsUnrecoverableError(Exception error)
        => error
            is OperationCanceledException // Required in any scenario here, otherwise RpcPeer.Run will run forever
            or ConnectionUnrecoverableException
            or TimeoutException;
}
