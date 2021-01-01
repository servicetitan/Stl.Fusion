using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class LogCommand : ICommand<Unit>
    {
        public string Message { get; set; } = "";
    }

    public class LogCommandHandler : ServiceBase, ICommandHandler<LogCommand>
    {
        public LogCommandHandler(IServiceProvider services) : base(services) { }

        public Task OnCommandAsync(
            LogCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation(command.Message);
            return Task.CompletedTask;
        }
    }
}
