using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.CommandLine
{
    public interface ICmd
    {
        ILogger Log { get; set; }
        ICliFormatter CliFormatter { get; set; }
        Func<CliString, CliString>? ArgumentTransformer { get; set; }
        CmdResultChecks ResultChecks { get; set; }
        bool EchoMode { get; set; }

        Task<ExecutionResult> RunRawAsync(
            CliString arguments, string? standardInput,
            CancellationToken cancellationToken = default);
    }

    public abstract class CmdBase : ICmd
    {
        public ILogger Log { get; set; } = NullLogger.Instance;
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public Func<CliString, CliString>? ArgumentTransformer { get; set; } = null;
        public CmdResultChecks ResultChecks { get; set; } = CmdResultChecks.NonZeroExitCode;
        public bool EchoMode { get; set; }

        public override string ToString() => $"{GetType().Name}()";

        protected Task<ExecutionResult> RunRawAsync(
            object? arguments, CliString tail = default,
            CancellationToken cancellationToken = default) 
            => RunRawAsync("", arguments, tail, cancellationToken);

        protected Task<ExecutionResult> RunRawAsync(
            CliString command, object? arguments, CliString tail = default, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + CliFormatter.Format(arguments) + tail, cancellationToken);

        public Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            CancellationToken cancellationToken = default)
            => RunRawAsync(arguments, null, cancellationToken);

        public virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken = default) 
        {
            arguments = TransformArguments(arguments);
            var command = $"{this} {arguments}"; 
            Log?.LogDebug($"Running: {command}");

            if (EchoMode) {
                var now = DateTimeOffset.Now;
                var executionResult = new ExecutionResult(0, command, "", now, now);
                return Task.FromResult(executionResult);
            } 
            
            return RunRawAsyncImpl(arguments, standardInput, cancellationToken);
        }

        protected abstract Task<ExecutionResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken);

        protected virtual CliString TransformArguments(CliString arguments)
            => ArgumentTransformer?.Invoke(arguments) ?? arguments;
    }
}
