using Stl.RegisterAttributes;

namespace Stl.Tests.CommandR.Services;

[RegisterService, RegisterCommandHandlers]
public class LogCommandHandler : ServiceBase, ICommandHandler<LogCommand>
{
    public LogCommandHandler(IServiceProvider services) : base(services) { }

    public Task OnCommand(
        LogCommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        var handler = context.ExecutionState.Handlers[^1];
        handler.GetType().Should().Be(typeof(InterfaceCommandHandler<LogCommand>));
        handler.Priority.Should().Be(0);

        Log.LogInformation("{Message}", command.Message);
        return Task.CompletedTask;
    }
}
