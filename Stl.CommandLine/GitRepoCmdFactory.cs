using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Stl.CommandLine.Ansible;
using Stl.CommandLine.Git;
using Stl.CommandLine.Python;
using Stl.CommandLine.Terraform;
using Stl.IO;
using Stl.OS;

namespace Stl.CommandLine
{
    public class GitRepoCmdFactory : CmdFactoryBase
    {
        public string DefaultRevision = "master";
        
        protected string GitRepositoryUrl { get; }
        protected string GitRevision { get; }
        protected string TargetPath { get; }
        protected string FilesPath { get; }
        protected string CommonPath { get; }
        protected string CommonBinPath { get; }
        protected string OSSpecificPath { get; }
        protected string OSSpecificBinPath { get; }
        protected bool AlwaysFetch { get; }

        public GitRepoCmdFactory(string repositoryUrl, string? revision = null, bool alwaysFetch = true)
            : this(PathEx.GetApplicationTempDirectory("Tools"), repositoryUrl, revision, alwaysFetch)
        { }

        public GitRepoCmdFactory(string targetPath, string gitRepositoryUrl, string? revision, bool alwaysFetch = true)
        {
            TargetPath = targetPath;
            FilesPath = Path.Combine(TargetPath, "Files");
            CommonPath = Path.Combine(FilesPath, "Common");
            CommonBinPath = Path.Combine(CommonPath, "bin");
            OSSpecificPath = Path.Combine(FilesPath, OSInfo.Kind.ToString());
            OSSpecificBinPath = Path.Combine(OSSpecificPath, "bin");
            GitRepositoryUrl = gitRepositoryUrl;
            GitRevision = revision ?? DefaultRevision;
            AlwaysFetch = alwaysFetch;
        }

        protected override void PopulateCommands(List<CmdDescriptor> commands)
        {
            // Just shortcuts
            void Add(CmdDescriptor cmdDescriptor) => commands.Add(cmdDescriptor);
            CliString OSSpecificBinPath(CliString exe) => Path.Combine(this.OSSpecificBinPath, exe.Value);
            CliString CommonBinPath(CliString exe) => Path.Combine(this.CommonBinPath, exe.Value);

            Add(CmdDescriptor.New(() => new ShellCmd(), "shell", "sh"));
            Add(CmdDescriptor.New(() => new GitCmd(), "git"));
            Add(CmdDescriptor.New(() => new Python2Cmd(), "python2", "py2"));
            Add(CmdDescriptor.New(() => new Python3Cmd(), "python3", "py3"));
            Add(CmdDescriptor.New(() => new TerraformCmd(OSSpecificBinPath(TerraformCmd.DefaultExecutable)), "terraform", "tf"));
            Add(CmdDescriptor.New(() => new AnsibleCmd(CommonBinPath(AnsibleCmd.DefaultAnsiblePath)), "ansible", "a"));
        }

        protected override async Task InitializeImplAsync()
        {
            var gitFetcher = new GitFetcher(GitRepositoryUrl, GitRevision, TargetPath) {
                AlwaysFetch = AlwaysFetch,
            };
            await gitFetcher.FetchAsync().ConfigureAwait(false);
        }
    }
}
