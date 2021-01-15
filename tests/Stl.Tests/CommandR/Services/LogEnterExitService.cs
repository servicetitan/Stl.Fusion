using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;

namespace Stl.Tests.CommandR.Services
{
    [Service, AddCommandHandlers]
    public class LogEnterExitService : ServiceBase
    {
        public LogEnterExitService(IServiceProvider services) : base(services) { }

        [CommandHandler(1000, IsFilter = true)]
        public async Task OnAnyCommandAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation($"+ {command}");
            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                await LogResultAsync((dynamic) command);
            }
            catch (Exception e) {
                Log.LogError($"- {command} !-> error: {e}");
                throw;
            }
        }

        protected async Task LogResultAsync<T>(ICommand<T> command)
        {
            var context = (CommandContext<T>) CommandContext.Current!;
            var resultTask = context.ResultTask;
            if (!resultTask.IsCompleted) {
                Log.LogInformation($"- {command} -> {default(T)}");
                return;
            }
            var result = await resultTask.ConfigureAwait(false);
            Log.LogInformation($"- {command} -> {result}");
        }
    }
}
