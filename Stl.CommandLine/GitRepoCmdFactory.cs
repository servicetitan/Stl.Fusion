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
        protected string BasePath { get; }
        protected string CommonPath { get; }
        protected string OSSpecificPath { get; }
        protected string RepositoryUrl { get; }
        protected string Revision { get; }
        protected bool AlwaysFetch { get; }

        public GitRepoCmdFactory(string repositoryUrl, string? revision = null, bool alwaysFetch = true)
            : this(PathEx.GetApplicationTempDirectory("Tools"), repositoryUrl, revision, alwaysFetch)
        { }

        public GitRepoCmdFactory(string repositoryPath, string repositoryUrl, string? revision, bool alwaysFetch = true)
        {
            RepositoryPath = repositoryPath;
            BasePath = Path.Combine(RepositoryPath);
            CommonPath = Path.Combine(BasePath, "Common");
            OSSpecificPath = Path.Combine(BasePath, OSInfo.Kind.ToString());
            RepositoryUrl = repositoryUrl;
            Revision = revision ?? DefaultRevision;
            AlwaysFetch = alwaysFetch;
        }

        protected override void PopulateFactories(Dictionary<object, Func<ICmd>> factories)
        {
            CliString OSSpecificPath(CliString exe) => Path.Combine(this.OSSpecificPath, exe.Value);
            CliString CommonPath(CliString exe) => Path.Combine(this.CommonPath, exe.Value);

            AddFactory(factories, () => new ShellCmd(), "shell", "sh");
            AddFactory(factories, () => new GitCmd(), "git");
            AddFactory(factories, () => new Python2Cmd(), "python2", "py2");
            AddFactory(factories, () => new Python3Cmd(), "python3", "py3");
            AddFactory(factories, () => new TerraformCmd(OSSpecificPath(TerraformCmd.DefaultExecutable)), "terraform", "tf");
            AddFactory(factories, () => new AnsibleCmd(CommonPath(AnsibleCmd.DefaultAnsiblePath)), "ansible", "a");
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
                        .RunAsync("fetch origin" + new CliString(Revision))
                        .ConfigureAwait(false);
                    await git
                        .RunAsync("checkout" + new CliString(Revision))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}