using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;

namespace Stl.CommandLine.Terraform
{
    public class TerraformCmd : CmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("terraform" + CmdHelpers.ExeExtension);

        public TerraformCmd(CliString? executable = null)
            : base(executable ?? DefaultExecutable)
        { }

        public Task<ExecutionResult> ApplyAsync(
            CliString dir = default,
            ApplyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("apply", arguments ?? new ApplyArguments(), dir, cancellationToken);

        public Task<ExecutionResult> ImportAsync(
            CliString dir = default,
            ImportArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("import", arguments ?? new ImportArguments(), dir, cancellationToken);

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

        public Task<ExecutionResult> NewWorkspaceAsync(
            CliString workspaceName,
            WorkspaceArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace new", arguments ?? new WorkspaceArguments(), workspaceName, cancellationToken);

        public Task<ExecutionResult> SelectWorkspaceAsync(
            CliString workspaceName,
            WorkspaceArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace select", arguments ?? new WorkspaceArguments(), workspaceName, cancellationToken);
    }
}
