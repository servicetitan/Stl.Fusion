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
        
        protected string RepositoryPath { get; }
        protected string FilesPath { get; }
        protected string CommonPath { get; }
        protected string CommonBinPath { get; }
        protected string OSSpecificPath { get; }
        protected string OSSpecificBinPath { get; }
        protected string RepositoryUrl { get; }
        protected string Revision { get; }
        protected bool AlwaysFetch { get; }

        public GitRepoCmdFactory(string repositoryUrl, string? revision = null, bool alwaysFetch = true)
            : this(PathEx.GetApplicationTempDirectory("Tools"), repositoryUrl, revision, alwaysFetch)
        { }

        public GitRepoCmdFactory(string repositoryPath, string repositoryUrl, string? revision, bool alwaysFetch = true)
        {
            RepositoryPath = repositoryPath;
            FilesPath = Path.Combine(RepositoryPath, "Files");
            CommonPath = Path.Combine(FilesPath, "Common");
            CommonBinPath = Path.Combine(CommonPath, "bin");
            OSSpecificPath = Path.Combine(FilesPath, OSInfo.Kind.ToString());
            OSSpecificBinPath = Path.Combine(OSSpecificPath, "bin");
            RepositoryUrl = repositoryUrl;
            Revision = revision ?? DefaultRevision;
            AlwaysFetch = alwaysFetch;
        }

        protected override void PopulateFactories(Dictionary<object, Func<ICmd>> factories)
        {
            CliString OSSpecificBinPath(CliString exe) => Path.Combine(this.OSSpecificBinPath, exe.Value);
            CliString CommonBinPath(CliString exe) => Path.Combine(this.CommonBinPath, exe.Value);

            AddFactory(factories, () => new ShellCmd(), "shell", "sh");
            AddFactory(factories, () => new GitCmd(), "git");
            AddFactory(factories, () => new Python2Cmd(), "python2", "py2");
            AddFactory(factories, () => new Python3Cmd(), "python3", "py3");
            AddFactory(factories, () => new TerraformCmd(OSSpecificBinPath(TerraformCmd.DefaultExecutable)), "terraform", "tf");
            AddFactory(factories, () => new AnsibleCmd(CommonBinPath(AnsibleCmd.DefaultAnsiblePath)), "ansible", "a");
        }

        protected override async Task InitializeImplAsync()
        {
            var git = new GitCmd();
            var gitFolder = Path.Combine(RepositoryPath, ".git");
            if (!Directory.Exists(gitFolder)) {
                if (!Directory.Exists(RepositoryPath))
                    Directory.CreateDirectory(RepositoryPath);
                git.WorkingDirectory = RepositoryPath;
                await git
                    .RunAsync("clone" + new CliString(RepositoryUrl).Quote() + ".")
                    .ConfigureAwait(false);
                await git
                    .RunAsync("checkout" + new CliString(Revision))
                    .ConfigureAwait(false);
            } 
            else {
                git.WorkingDirectory = RepositoryPath;
                await git
                    .RunAsync("reset --hard HEAD")
                    .ConfigureAwait(false);
                var mustFetch = AlwaysFetch;
                using (git.OpenErrorValidationScope(false)) {
                    var r = await git
                        .RunAsync("checkout" + new CliString(Revision))
                        .ConfigureAwait(false);
                    mustFetch |= r.ExitCode != 0;
                }
                if (mustFetch) {
                    await git
                        .RunAsync("fetch origin")
                        .ConfigureAwait(false);
                    await git
                        .RunAsync("checkout" + new CliString(Revision))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
