using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.Time;

namespace Stl.Fusion.Operations.Internal
{
    /// <summary>
    /// This scope serves as the outermost, "catch-all" operation scope for
    /// commands that don't use any other scopes.
    /// </summary>
    public class TransientOperationScope : AsyncDisposableBase, IOperationScope
    {
        protected IServiceProvider Services { get; }
        protected AgentInfo AgentInfo { get; }
        protected IMomentClock Clock { get; }
        protected ILogger Log { get; }

        IOperation IOperationScope.Operation => Operation;
        public TransientOperation Operation { get; }
        public CommandContext CommandContext { get; }
        public bool IsUsed => CommandContext.Items.TryGet<ICompletion>() == null;
        public bool IsClosed { get; private set; }
        public bool? IsConfirmed { get; private set; }

        public TransientOperationScope(IServiceProvider services)
        {
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
            Services = services;
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            AgentInfo = services.GetRequiredService<AgentInfo>();
            Operation = new TransientOperation(true) {
                AgentId = AgentInfo.Id,
                StartTime = Clock.Now,
            };
            CommandContext = services.GetRequiredService<CommandContext>();
        }

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            IsConfirmed ??= true;
            IsClosed = true;
            return ValueTaskEx.CompletedTask;
        }

        public virtual Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsClosed)
                throw Errors.OperationScopeIsAlreadyClosed();
            Operation.CommitTime = Clock.Now;
            IsConfirmed = true;
            IsClosed = true;
            return Task.CompletedTask;
        }

        public virtual Task RollbackAsync()
        {
            if (IsClosed)
                throw Errors.OperationScopeIsAlreadyClosed();
            IsConfirmed = false;
            IsClosed = true;
            return Task.CompletedTask;
        }
    }
}
