using System;
using System.Reactive;
using System.Threading;
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
        public static AsyncLocal<object> Tag { get; } = new();

        public double[] Arguments { get; set; } = Array.Empty<double>();
        public bool Isolate { get; set; }
    }

    public class RecAddUsersCommand : ICommand<Unit>
    {
        public User[] Users { get; set; } = Array.Empty<User>();
    }
}
