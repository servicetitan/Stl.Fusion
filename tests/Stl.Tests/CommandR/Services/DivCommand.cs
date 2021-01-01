using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class DivCommand : ICommand<double>
    {
        public double Divisible { get; set; }
        public double Divisor { get; set; }
    }

    public class DivCommandHandler : ServiceBase, ICommandHandler<DivCommand, double>
    {
        public DivCommandHandler(IServiceProvider services) : base(services) { }

        public Task<double> OnCommandAsync(
            DivCommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            Log.LogInformation($"{command.Divisible} / {command.Divisor} =");
            var result = command.Divisible / command.Divisor;
            Log.LogInformation($"  {result}");
            if (double.IsInfinity(result))
                throw new DivideByZeroException();
            return Task.FromResult(result);
        }
    }
}
