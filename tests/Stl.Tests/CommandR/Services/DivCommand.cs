using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class DivCommand : ICommand<double>
    {
        public double Divisible { get; set; }
        public double Divisor { get; set; }
    }

    public class DivCommandHandler : CommandHandlerBase<DivCommand>
    {
        public DivCommandHandler(IServiceProvider services) : base(services) { }

        public override Task OnCommandAsync(
            DivCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation($"{command.Divisible} / {command.Divisor} =");
            var result = command.Divisible / command.Divisor;
            Log.LogInformation($"  {result}");
            if (double.IsInfinity(result))
                throw new DivideByZeroException();

            context.Cast<double>().SetResult(result);
            return Task.CompletedTask;
        }
    }
}
