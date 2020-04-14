using System;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Exceptions;
using Stl.Async;
using Stl.IO;
using Stl.Locking;

namespace Stl.CommandLine.Git
{
    public class GitFetcher
    {
        public bool AlwaysFetch { get; set; } = false;
        public string SourceUrl { get; set; }
        public string SourceRevision { get; set; }
        public PathString TargetPath { get; set; }
        public Func<GitCmd> GitCmdFactory { get; set; }

        public GitFetcher(string sourceUrl, string sourceRevision, PathString targetPath, 
            Func<GitCmd>? gitCmdFactory = null)
        {
            SourceUrl = sourceUrl;
            SourceRevision = sourceRevision;
            TargetPath = targetPath;
            GitCmdFactory = gitCmdFactory ?? (() => new GitCmd());
        }

        public async Task FetchAsync(CancellationToken cancellationToken = default)
        {
            using var _ = await FileLock
                .LockAsync(TargetPath + ".lock", cancellationToken)
                .ConfigureAwait(false);
            
            async Task TryAsync()
            {
                var git = GitCmdFactory.Invoke();
                git.ResultValidation = CommandResultValidation.ZeroExitCode;

                var gitFolder = TargetPath & ".git";
                if (!Directory.Exists(gitFolder)) {
                    if (!Directory.Exists(TargetPath))
                        Directory.CreateDirectory(TargetPath);
                    git.WorkingDirectory = ""; // It has to be non-target path on cloning
                    await git
                        .RunAsync("clone" + CliString.Quote(SourceUrl) + CliString.Quote(TargetPath), cancellationToken)
                        .ConfigureAwait(false);
                    git.WorkingDirectory = TargetPath;
                    await git
                        .RunAsync("checkout" + new CliString(SourceRevision), cancellationToken)
                        .ConfigureAwait(false);
                } 
                else {
                    git.WorkingDirectory = TargetPath;
                    var mustFetch = AlwaysFetch;
                    if (!AlwaysFetch) {
                        using (git.ChangeResultValidation(0)) {
                            var r = await git
                                .RunAsync("rev-parse --short=10 HEAD", cancellationToken)
                                .ConfigureAwait(false);
                            mustFetch |= r.ExitCode != 0;
                            mustFetch |= r.StandardOutput.Trim() != SourceRevision;
                        }
                    }
                    if (mustFetch) {
                        await git
                            .RunAsync("reset --hard HEAD", cancellationToken)
                            .ConfigureAwait(false);
                        await git
                            .RunAsync("fetch origin", cancellationToken)
                            .ConfigureAwait(false);
                        await git
                            .RunAsync("checkout" + new CliString(SourceRevision), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            try {
                await TryAsync().ConfigureAwait(false);
            }
            catch (CommandExecutionException e) {
                if (!e.Message.Contains("non-zero exit code"))
                    throw;
                Directory.Delete(TargetPath, true);
                await TryAsync().ConfigureAwait(false);
            }
        }
    }
}
