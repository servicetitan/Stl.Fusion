using Stl.Rpc;

namespace Stl.Tests.Rpc;

public interface ISimpleRpcService : ICommandService
{
    Task<int?> Div(int? a, int b);
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task OnDummyCommand(DummyCommand command, CancellationToken cancellationToken = default);

    [DataContract]
    public record DummyCommand([property: DataMember] string Input) : ICommand<Unit>;
}

public interface ISimpleRpcServiceClient : ISimpleRpcService, IRpcService
{ }

public class SimpleRpcService : ISimpleRpcService
{
    public Task<int?> Div(int? a, int b)
        => Task.FromResult(a / b);

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.Delay(duration, cancellationToken);

    public virtual Task OnDummyCommand(ISimpleRpcService.DummyCommand command, CancellationToken cancellationToken = default)
    {
        if (Equals(command.Input, "error"))
            throw new ArgumentOutOfRangeException(nameof(command));
        return Task.CompletedTask;
    }
}
