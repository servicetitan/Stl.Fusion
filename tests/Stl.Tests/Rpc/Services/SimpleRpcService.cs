using Stl.Rpc;

namespace Stl.Tests.Rpc;

public interface ISimpleRpcService
{
    Task<int?> Div(int? a, int b);
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
}

public interface ISimpleRpcServiceClient : ISimpleRpcService, IRpcService
{ }

public class SimpleRpcService : ISimpleRpcService
{
    public Task<int?> Div(int? a, int b)
        => Task.FromResult(a / b);

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.Delay(duration, cancellationToken);

}
