namespace Stl.Tests.CommandR.Services;

public class LogCommandHandler : ServiceBase, ICommandHandler<LogCommand>, ICommandHandler<LogEvent>
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

    public Task OnCommand(
        LogEvent command, CommandContext context,
        CancellationToken cancellationToken)
    {
        var handler = context.ExecutionState.Handlers[^1];
        handler.GetType().Should().Be(typeof(InterfaceCommandHandler<LogEvent>));
        handler.Priority.Should().Be(0);

        Log.LogInformation("{Message}", command.Message);
        return Task.CompletedTask;
    }
}
