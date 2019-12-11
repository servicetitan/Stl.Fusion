using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using SysProcess=System.Diagnostics.Process;

namespace Stl.CommandLine.Consul 
{
    public class ConsulServiceCmd : ProxyCmd<ConsulCmd>
    {
        public TimeSpan CheckDelay { get; set; } = TimeSpan.FromSeconds(10);

        public ConsulServiceCmd(Func<ConsulCmd> consulCmdFactory) : base(consulCmdFactory) { }

        protected override async Task<ExecutionResult> RunRawAsyncImpl(CliString arguments, string? standardInput, CancellationToken cancellationToken)
        {
            var consulIsRunning = SysProcess.GetProcesses()
                .Any(p => p.ProcessName == "consul" || p.ProcessName == "consul.exe");
            var now = DateTimeOffset.Now;
            if (consulIsRunning)
                return new ExecutionResult(0, "Consul service is already running.", "", now, now);

            var targetCmd = TargetCmdFactory.Invoke();
            // If the repo w/ Consul is supposed to be fetched, it is fetched at this point
            targetCmd.EchoMode = EchoMode;
            targetCmd.ResultChecks = ResultChecks;
            var consulTask = targetCmd.RunRawAsync(arguments, standardInput, cancellationToken);
            var completedTask = await Task.WhenAny(consulTask, Task.Delay(CheckDelay, cancellationToken));
            if (completedTask == consulTask)
                return await consulTask;
            return new ExecutionResult(0, "Consul service seems to be started successfully.", "", now, now);
        }
    }
}
