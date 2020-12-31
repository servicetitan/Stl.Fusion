using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public abstract class CommandHandlerBase<TCommand> : ICommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }

        protected CommandHandlerBase(IServiceProvider services)
        {
            Services = services;
            Log = services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
        }

        public abstract Task OnCommandAsync(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken);
    }
}
