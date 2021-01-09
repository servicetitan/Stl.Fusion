using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.Tests.CommandR.Services
{
    [CommandService]
    public class MathService : ServiceBase
    {
        public MathService(IServiceProvider services) : base(services) { }

        [CommandHandler(Order = 2)]
        private Task<double> OnCommandAsync(DivCommand command, CommandContext<double> context, CancellationToken cancellationToken)
        {
            var handler = context.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<DivCommand>));
            handler.Order.Should().Be(2);

            Log.LogInformation($"{command.Divisible} / {command.Divisor} =");
            var result = command.Divisible / command.Divisor;
            Log.LogInformation($"  {result}");
            if (double.IsInfinity(result))
                throw new DivideByZeroException();
            return Task.FromResult(result);
        }

        [CommandHandler(Order = 1)]
        public virtual async Task<double> RecSumAsync(RecSumCommand command, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.GetCurrent<double>();
            var handler = context.Handlers[^1];
            handler.GetType().Should().Be(typeof(MethodCommandHandler<RecSumCommand>));
            handler.Order.Should().Be(1);

            Log.LogInformation($"Arguments: {command.Arguments.ToDelimitedString()}");

            if (command.Arguments.Length == 0)
                return 0;

            var tailSum = await RecSumAsync(
                new RecSumCommand() { Arguments = command.Arguments[1..] },
                cancellationToken)
                .ConfigureAwait(false);
            return command.Arguments[0] + tailSum;
        }
    }
}
