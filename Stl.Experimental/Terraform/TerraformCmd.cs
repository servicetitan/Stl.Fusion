using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Stl.CommandLine;

namespace Stl.Terraform
{
    public class TerraformCmd : Cmd
    {
        public static readonly CliString DefaultExecutable =
            CliString.New("terraform").VaryByOS("terraform.exe");

        public TerraformCmd(
            CliString? executable = null,
            CliString? argumentsPrefix = null,
            ICliFormatter? cliFormatter = null)
            : base(
                executable ?? DefaultExecutable,
                argumentsPrefix ?? "",
                cliFormatter)
        {
        }

        public Task<ExecutionResult> ApplyAsync(
            CliString? dir = null,
            CliString? workingDirectory = null,
            ApplyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("apply", arguments ?? new ApplyArguments(), workingDirectory ,dir, cancellationToken);

        public Task<ExecutionResult> FmtAsync(
            CliString? dir = null,
            CliString? workingDirectory = null,
            FmtArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("fmt", arguments, workingDirectory,dir, cancellationToken);

        public Task<ExecutionResult> InitAsync(
            CliString? dir = null,
            CliString? workingDirectory = null,
            InitArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("init", arguments, workingDirectory, dir, cancellationToken);

        public Task<ExecutionResult> DestroyAsync(
            CliString? dir = null,
            CliString? workingDirectory= null,
            DestroyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("destroy", arguments ?? new DestroyArguments(),
                workingDirectory, dir, cancellationToken);
    }
}