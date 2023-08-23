using Stl.Interception;
using ServiceProviderExt = Stl.CommandR.ServiceProviderExt;

namespace Stl.Tests.CommandR.Services;

public interface IMathService : ICommandService, IRequiresFullProxy
{
    [CommandHandler(Priority = 1)]
    Task<double> RecSum(RecSumCommand command, CancellationToken cancellationToken = default);
}

public class MathService(IServiceProvider services) : ServiceBase(services), IMathService
{
    private readonly object _lock = new();

    private ICommander Commander { get; } = services.Commander();

    public long Value { get; set; }

    [CommandHandler(Priority = 2)]
    protected virtual Task<double> Divide(DivCommand command, CancellationToken cancellationToken)
    {
        var context = CommandContext.GetCurrent<double>();
        var handler = context.ExecutionState.Handlers[^1];
        handler.GetType().Should().Be(typeof(MethodCommandHandler<DivCommand>));
        handler.Priority.Should().Be(2);

        Log.LogInformation("{Divisible} / {Divisor} =", command.Divisible, command.Divisor);
        var result = command.Divisible / command.Divisor;
        Log.LogInformation("  {Result}", result);
        if (double.IsInfinity(result))
            throw new DivideByZeroException();
        return Task.FromResult(result);
    }

    public virtual async Task<double> RecSum(RecSumCommand command, CancellationToken cancellationToken = default)
    {
        var context = CommandContext.GetCurrent<double>();
        var handler = context.ExecutionState.Handlers[^1];
        handler.GetType().Should().Be(typeof(MethodCommandHandler<RecSumCommand>));
        handler.Priority.Should().Be(1);
        if (context.IsOutermost) {
            RecSumCommand.Tag.Value.Should().BeNull();
            RecSumCommand.Tag.Value = new();
        }
        else {
            RecSumCommand.Tag.Value.Should().NotBeNull();
        }

        Log.LogInformation("Arguments: {Arguments}", command.Arguments.ToDelimitedString());

        if (command.Arguments.Length == 0)
            return 0;

        var tailCommand = new RecSumCommand() {
            Arguments = command.Arguments[1..],
        };

        var tailSum = await Commander.Call(tailCommand, cancellationToken).ConfigureAwait(false);
        return command.Arguments[0] + tailSum;
    }

    [CommandHandler]
    public virtual async Task<Unit> Set(IncSetFailCommand command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(command.SetDelay, cancellationToken).ConfigureAwait(false);
        Log.LogInformation("Set: ChainId = {ChainId}", command.ChainId);
        command.ChainId.IsEmpty.Should().BeFalse();
        if (command.SetValue is { } value) {
            lock (_lock)
                Value = value;
        }

        return default;
    }

    [CommandHandler]
    public virtual async Task Inc(IncSetFailCommand command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(command.IncrementDelay, cancellationToken).ConfigureAwait(false);
        Log.LogInformation("Inc: ChainId = {ChainId}", command.ChainId);
        command.ChainId.IsEmpty.Should().BeFalse();
        lock (_lock) {
            Value += command.IncrementBy;
        }
    }

    [CommandHandler]
    public virtual async Task Fail(IncSetFailCommand command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(command.FailDelay, cancellationToken).ConfigureAwait(false);
        Log.LogInformation("Fail: ChainId = {ChainId}", command.ChainId);
        command.ChainId.IsEmpty.Should().BeFalse();
        if (command.MustFail)
            throw new InvalidOperationException("Fail!");
    }
}
