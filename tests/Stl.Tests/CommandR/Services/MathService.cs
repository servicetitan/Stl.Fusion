using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Tests.CommandR.Services
{
    public class MathService : ServiceBase
    {
        public MathService(IServiceProvider services) : base(services) { }

        [CommandHandler(Priority = 11)]
        public Task<double> DivAsync(DivCommand command, CancellationToken cancellationToken)
        {
            var context = CommandContext.GetCurrent<double>();
            context.Handlers[^1].Priority.Should().Be(11);

            Log.LogInformation($"{command.Divisible} / {command.Divisor} =");
            var result = command.Divisible / command.Divisor;
            Log.LogInformation($"  {result}");
            if (double.IsInfinity(result))
                throw new DivideByZeroException();
            return Task.FromResult(result);
        }

        public async Task<double> RecSumAsync(
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
