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

    public class LogCommandHandler : CommandHandlerBase<LogCommand>
    {
        public LogCommandHandler(IServiceProvider services) : base(services) { }

        public override Task OnCommandAsync(
            LogCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation(command.Message);
            return Task.CompletedTask;
        }
    }
}
