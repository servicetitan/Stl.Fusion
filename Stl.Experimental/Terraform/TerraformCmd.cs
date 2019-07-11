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

        public TerraformCmd(CliString? executable = null)
            : base(executable ?? DefaultExecutable)
        { }

        public Task<ExecutionResult> ApplyAsync(
            CliString dir = default,
            ApplyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("apply", arguments ?? new ApplyArguments(), dir, cancellationToken);

        public Task<ExecutionResult> FmtAsync(
            CliString dir = default,
            FmtArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("fmt", arguments ?? new FmtArguments(), dir, cancellationToken);

        public Task<ExecutionResult> InitAsync(
            CliString dir = default,
            InitArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("init", arguments ?? new InitArguments(), dir, cancellationToken);

        public Task<ExecutionResult> DestroyAsync(
            CliString dir = default,
            DestroyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("destroy", arguments ?? new DestroyArguments(), dir, cancellationToken);
    }
}
