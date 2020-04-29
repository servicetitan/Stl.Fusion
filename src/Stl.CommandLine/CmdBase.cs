using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.CommandLine
{
    public interface ICmd
    {
        ILogger Log { get; set; }
        ICliFormatter CliFormatter { get; set; }
        Func<CliString, CliString>? ArgumentTransformer { get; set; }
        CommandResultValidation ResultValidation { get; set; }
        bool EchoMode { get; set; }

        Task<CmdResult> RunRawAsync(
            CliString arguments, string? standardInput,
            CancellationToken cancellationToken = default);
    }

    public abstract class CmdBase : ICmd
    {
        public ILogger Log { get; set; } = NullLogger.Instance;
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public Func<CliString, CliString>? ArgumentTransformer { get; set; } = null;
        public CommandResultValidation ResultValidation { get; set; } = CommandResultValidation.ZeroExitCode;
        public bool EchoMode { get; set; }

        public override string ToString() => $"{GetType().Name}()";

        protected Task<CmdResult> RunRawAsync(
            object? arguments, CliString tail = default,
            CancellationToken cancellationToken = default) 
            => RunRawAsync("", arguments, tail, cancellationToken);

        protected Task<CmdResult> RunRawAsync(
            CliString command, object? arguments, CliString tail = default, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + CliFormatter.Format(arguments) + tail, cancellationToken);

        public Task<CmdResult> RunRawAsync(
            CliString arguments,
            CancellationToken cancellationToken = default)
            => RunRawAsync(arguments, null, cancellationToken);

        public virtual Task<CmdResult> RunRawAsync(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken = default) 
        {
            arguments = TransformArguments(arguments);
            var command = $"{this} {arguments}"; 
            Log?.LogDebug($"Running: {command}");

            if (EchoMode) {
                var now = DateTimeOffset.Now;
                var executionResult = new CmdResult(null!, 0, now, now, command);
                return Task.FromResult(executionResult);
            } 
            
            return RunRawAsyncImpl(arguments, standardInput, cancellationToken);
        }

        protected abstract Task<CmdResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken);

        protected virtual CliString TransformArguments(CliString arguments)
            => ArgumentTransformer?.Invoke(arguments) ?? arguments;
    }
}
