namespace Stl.Fusion.Operations.Internal;

/// <summary>
/// This scope serves as the outermost, "catch-all" operation scope for
/// commands that don't use any other scopes.
/// </summary>
public sealed class TransientOperationScope : AsyncDisposableBase, IOperationScope
{
    private IServiceProvider Services { get; }
    private AgentInfo AgentInfo { get; }
    private MomentClockSet Clocks { get; }
    private ILogger Log { get; }

    IOperation IOperationScope.Operation => Operation;
    public TransientOperation Operation { get; }
    public CommandContext CommandContext { get; }
    public bool IsUsed { get; private set; }
    public bool IsClosed { get; private set; }
    public bool? IsConfirmed { get; private set; }

    public TransientOperationScope(IServiceProvider services)
    {
        var loggerFactory = services.GetService<ILoggerFactory>();
        Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        Services = services;
        Clocks = services.Clocks();
        AgentInfo = services.GetRequiredService<AgentInfo>();
        Operation = new TransientOperation(true) {
            AgentId = AgentInfo.Id,
            StartTime = Clocks.SystemClock.Now,
        };
        CommandContext = CommandContext.GetCurrent();
    }

    protected override ValueTask DisposeAsyncCore()
    {
        IsConfirmed ??= true;
        IsClosed = true;
        return ValueTaskExt.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        // Intentionally ignore disposing flag here
        IsConfirmed ??= true;
        IsClosed = true;
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Close(true);
        return Task.CompletedTask;
    }

    public Task Rollback()
    {
        Close(false);
        return Task.CompletedTask;
    }

    public void Close(bool isConfirmed)
    {
        if (IsClosed)
            return;

        if (isConfirmed)
            Operation.CommitTime = Clocks.SystemClock.Now;
        IsConfirmed = isConfirmed;
        IsClosed = true;
        IsUsed = CommandContext.Items.Replace<IOperationScope?>(null, this);
    }
}
