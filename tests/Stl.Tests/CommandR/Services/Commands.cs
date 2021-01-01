using System;
using System.Reactive;
using Stl.CommandR;

namespace Stl.Tests.CommandR.Services
{
    public class LogCommand : ICommand<Unit>
    {
        public string Message { get; set; } = "";
    }

    public class DivCommand : ICommand<double>
    {
        public double Divisible { get; set; }
        public double Divisor { get; set; }
    }

    public class RecSumCommand : ICommand<double>
    {
        public double[] Arguments { get; set; } = Array.Empty<double>();
    }
}
