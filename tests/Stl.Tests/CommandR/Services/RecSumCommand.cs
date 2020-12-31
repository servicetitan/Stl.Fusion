using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Collections;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class RecSumCommand : ICommand<double>
    {
        public double[] Arguments { get; set; } = Array.Empty<double>();
    }

    public class RecSumCommandHandler : CommandHandlerBase<RecSumCommand>
    {
        public RecSumCommandHandler(IServiceProvider services) : base(services) { }

        public override async Task OnCommandAsync(
            RecSumCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var typedContext = context.Cast<double>();
            Log.LogInformation($"Arguments: {command.Arguments.ToDelimitedString()}");
            typedContext.Should().BeSameAs(CommandContext.GetCurrent());

            if (command.Arguments.Length == 0) {
                typedContext.SetResult(0);
                return;
            }

            var tailSum = await Services.CommandDispatcher().DispatchAsync(
                new RecSumCommand() { Arguments = command.Arguments[1..] },
                cancellationToken)
                .ConfigureAwait(false);
            typedContext.TrySetResult(command.Arguments[0] + tailSum);
        }
    }
}
