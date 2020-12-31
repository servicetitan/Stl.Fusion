using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class LogEnterExitHandler : CommandHandlerBase<ICommand>
    {
        public LogEnterExitHandler(IServiceProvider services) : base(services) { }

        public override async Task OnCommandAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation($"+ {command}");
            try {
                await context.InvokeNextHandlerAsync(cancellationToken).ConfigureAwait(false);
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
