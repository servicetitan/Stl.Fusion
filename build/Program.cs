using System.Linq;
using System.Threading.Tasks;
using ServiceTitan.Platform.Build;
using System;

namespace Stl.Build
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            //var version = Nerdbank.GitVersioning.VersionFile.
            return await new BuildRunnerBuilder()
                .SetCommandLineArgs(args)
                .UseNerdbankGitVersioning()
                .Build()
                .RunAsync().ConfigureAwait(false);
        }
    }
}
