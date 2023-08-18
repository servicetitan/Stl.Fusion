using Stl.Rpc;

namespace Stl.Tests.Rpc;

public interface ITestRpcBackend : ICommandService, IBackendService
{
    Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default);
}

public interface ITestRpcBackendClient : ITestRpcBackend, IRpcService
{ }

public class TestRpcBackend : ITestRpcBackend
{
    public Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default)
        => Task.FromResult(argument);
}
