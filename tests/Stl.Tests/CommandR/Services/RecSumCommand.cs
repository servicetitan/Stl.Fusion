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

    public class RecSumCommandHandler : ServiceBase, ICommandHandler<RecSumCommand, double>
    {
        public RecSumCommandHandler(IServiceProvider services) : base(services) { }

        public async Task<double> OnCommandAsync(
            RecSumCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var typedContext = context.Cast<double>();
            Log.LogInformation($"Arguments: {command.Arguments.ToDelimitedString()}");
            typedContext.Should().BeSameAs(CommandContext.GetCurrent());

            if (command.Arguments.Length == 0)
                return 0;

            var tailSum = await Services.CommandDispatcher().RunAsync(
                new RecSumCommand() { Arguments = command.Arguments[1..] },
                cancellationToken)
                .ConfigureAwait(false);
            return command.Arguments[0] + tailSum;
        }
    }
}
