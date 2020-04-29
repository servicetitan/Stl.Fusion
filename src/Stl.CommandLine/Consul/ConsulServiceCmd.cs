using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using SysProcess=System.Diagnostics.Process;

namespace Stl.CommandLine.Consul 
{
    public class ConsulServiceCmd : ProxyCmd<ConsulCmd>
    {
        public TimeSpan CheckDelay { get; set; } = TimeSpan.FromSeconds(10);

        public ConsulServiceCmd(Func<ConsulCmd> consulCmdFactory) : base(consulCmdFactory) { }

        protected override async Task<CmdResult> RunRawAsyncImpl(CliString arguments, string? standardInput, CancellationToken cancellationToken)
        {
            var consulIsRunning = SysProcess.GetProcesses()
                .Any(p => p.ProcessName == "consul" || p.ProcessName == "consul.exe");
            var now = DateTimeOffset.Now;
            if (consulIsRunning)
                return new CmdResult(null!, 0, now, now, "Consul service is already running.");

            var targetCmd = TargetCmdFactory.Invoke();
            // If the repo w/ Consul is supposed to be fetched, it is fetched at this point
            targetCmd.EchoMode = EchoMode;
            targetCmd.ResultValidation = ResultValidation;
            var consulTask = targetCmd.RunRawAsync(arguments, standardInput, cancellationToken);
            var completedTask = await Task.WhenAny(consulTask, Task.Delay(CheckDelay, cancellationToken));
            if (completedTask == consulTask)
                return await consulTask;
            return new CmdResult(null!, 0, now, now, "Consul service seems to be started successfully.");
        }
    }
}
