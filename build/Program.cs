using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Bullseye;
using CliWrap;
using CliWrap.Buffered;
using Stl.IO;
using static Bullseye.Targets;

namespace Build
{
    // DragonFruit isn't the best option here (e.g. currently it doesn't support aliases),
    // but it does the job.
    internal static class Program
    {
        /// <summary>Build project for this repository.</summary>
        /// <param name="arguments">A list of targets to run or list.</param>
        /// <param name="clear">Clear the console before execution.</param>
        /// <param name="dryRun">Do a dry run without executing actions.</param>
        /// <param name="host">Force the mode for a specific host environment (normally auto-detected).</param>
        /// <param name="listDependencies">List all (or specified) targets and dependencies, then exit.</param>
        /// <param name="listInputs">List all (or specified) targets and inputs, then exit.</param>
        /// <param name="listTargets">List all (or specified) targets, then exit.</param>
        /// <param name="listTree">List all (or specified) targets and dependency trees, then exit.</param>
        /// <param name="noColor">Disable colored output.</param>
        /// <param name="parallel">Run targets in parallel.</param>
        /// <param name="skipDependencies">Do not run targets' dependencies.</param>
        /// <param name="verbose">Enable verbose output.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="configuration">The configuration for building</param>
        /// <param name="framework">The framework to build for</param>
        /// <param name="isPublicRelease">You can redefine PublicRelease property for Nerdbank.GitVersioning</param>
        private static async Task Main(
            string[] arguments,
            bool clear,
            bool dryRun,
            Host host,
            bool listDependencies,
            bool listInputs,
            bool listTargets,
            bool listTree,
            bool noColor,
            bool parallel,
            bool skipDependencies,
            bool verbose,
            CancellationToken cancellationToken,
            // Our own options
            string configuration = "",
            string framework = "",
            bool isPublicRelease = false)
        {
            SetDefaults("Stl.Fusion.sln");
            var options = new Options {
                Clear = clear,
                DryRun = dryRun,
                Host = host,
                ListDependencies = listDependencies,
                ListInputs = listInputs,
                ListTargets = listTargets,
                ListTree = listTree,
                NoColor = noColor,
                Parallel = parallel,
                SkipDependencies = skipDependencies,
                Verbose = verbose,
            };

            var artifactsPath = FilePath.New("artifacts").FullPath;
            var nupkgPath = artifactsPath & "nupkg";
            var testOutputPath = artifactsPath & "tests" & "output";
            var dotnetExePath = TryFindDotNetExePath() ?? throw new FileNotFoundException(
                "'dotnet' command isn't found. Use DOTNET_ROOT env. var to specify the path to custom 'dotnet' tool.");

            // For Nerdbank.GitVersioning: https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/public_vs_stable.md
            var isPublicReleaseOverride = bool.TryParse(Environment.GetEnvironmentVariable("NBGV_PublicRelease")?.Trim() ?? "", out var v) ? (bool?) v : null;
            var publicReleaseProperty = $"-p:PublicRelease={isPublicReleaseOverride ?? isPublicRelease} ";

            Target("clean", () => {
                DeleteDir(artifactsPath);
                CreateDir(nupkgPath, true);
            });

            Target("clean-nupkg", () => {
                DeleteDir(nupkgPath);
                CreateDir(nupkgPath, true);
            });

            Target("restore-tools", async () => {
                await Cli.Wrap(dotnetExePath).WithArguments(new[] {"tool", "restore", "--ignore-failed-sources"})
                    .ToConsole()
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);
            });

            Target("restore", async () => {
                await Cli.Wrap(dotnetExePath).WithArguments(new[] {
                        "msbuild",
                        "-noLogo",
                        "-t:Restore",
                        "-p:RestoreForce=true",
                        "-p:RestoreIgnoreFailedSources=True",
                        publicReleaseProperty
                    }).ToConsole()
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);
            });

            Target("build", async () => {
                await Cli.Wrap(dotnetExePath).WithArguments(args => args
                        .Add("build")
                        .Add("-noLogo")
                        .AddOption("-c", configuration)
                        .AddOption("-f", framework)
                        .Add("--no-restore")
                        .Add(publicReleaseProperty)
                    )
                    .ToConsole()
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);
            });

            // Technically it should depend on "build" target, but such a setup fails
            // due to https://github.com/dotnet/orleans/issues/6073 ,
            // that's why we make "pack" to run "build" too here
            Target("pack", DependsOn("clean", "restore"), async () => {
                await Cli.Wrap(dotnetExePath).WithArguments(args => args
                        .Add("pack")
                        .Add("-noLogo")
                        .AddOption("-c", configuration)
                        .AddOption("-f", framework)
                        .Add("--no-restore")
                        .Add(publicReleaseProperty)
                    )
                    .ToConsole()
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);
            });

            Target("publish", DependsOn("clean-nupkg", "pack"), async () => {
                const string feed = "https://api.nuget.org/v3/index.json";
                var nugetOrgApiKey = Environment.GetEnvironmentVariable("NUGET_ORG_API_KEY") ?? "";
                if (string.IsNullOrWhiteSpace(nugetOrgApiKey))
                    throw new InvalidOperationException("NUGET_ORG_API_KEY env. var isn't set.");
                var nupkgPaths = Directory
                    .EnumerateFiles(nupkgPath.FullPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                    .Select(FilePath.New)
                    .ToArray();
                foreach (var nupkgPath in nupkgPaths) {
                    await Cli.Wrap(dotnetExePath).WithArguments(new string[] {
                            "nuget",
                            "push",
                            nupkgPath,
                            "--force-english-output",
                            "--timeout", "60",
                            "--api-key", nugetOrgApiKey,
                            "--source", feed,
                            "--skip-duplicate"
                        })
                        .ToConsole()
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            });

            Target("coverage", DependsOn("build"), async () => {
                CreateDir(testOutputPath);
                var cmd = await Cli.Wrap(dotnetExePath)
                    .WithArguments(args => args
                        .Add("test")
                        .Add("--nologo")
                        .Add("--no-restore")
                        .Add("--blame")
                        .Add("--collect:\"XPlat Code Coverage\"")
                        .Add("--results-directory").Add(testOutputPath)
                        .AddOption("-c", configuration)
                        .AddOption("-f", framework)
                        .Add("--")
                        .Add("DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura")
                    ).ToConsole()
                    .ExecuteBufferedAsync(cancellationToken)
                    .ConfigureAwait(false);

                MoveCoverageOutputFiles(testOutputPath);

                // Removes all files in inner folders, workaround for https://github.com/microsoft/vstest/issues/2334
                foreach (var path in Directory.EnumerateDirectories(testOutputPath).Select(FilePath.New))
                    DeleteDir(path);
            });

            Target("default", DependsOn("build"));

            try {
                // RunTargetsAndExitAsync hangs Target on Ctrl+C
                await RunTargetsWithoutExitingAsync(arguments, options, ex => ex is OperationCanceledException).ConfigureAwait(false);
            }
            catch (TargetFailedException tfe) {
                if (tfe.InnerException is OperationCanceledException oce) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(oce.Message);
                    Console.ResetColor();
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception: {ex}");
                Console.ResetColor();
            }
        }

        private static void SetDefaults(string solutionName)
        {
            var slnPath = FindNearest(Environment.CurrentDirectory, solutionName)
                ?? FindNearest(FilePath.New(Assembly.GetExecutingAssembly().Location).DirectoryPath, solutionName)
                ?? throw new InvalidOperationException($"Can't find '{solutionName}'.");

            Environment.CurrentDirectory = slnPath.DirectoryPath;
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_SVCUTIL_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
            Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
            Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("POWERSHELL_UPDATECHECK_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en");
            Environment.SetEnvironmentVariable("PUBLIC_BUILD", "1");
        }

        static void MoveCoverageOutputFiles(FilePath testOutputPath)
        {
            // Moves coverage reports from GUID folders to the output path. A workaround for:
            // - https://github.com/microsoft/vstest/issues/2378
            // - https://github.com/microsoft/vstest/issues/2334
            var dirPaths = (
                from dirPath in Directory.EnumerateDirectories(testOutputPath).Select(FilePath.New)
                let createTime = Directory.GetCreationTime(dirPath)
                orderby createTime
                select dirPath
                ).ToArray();
            var dirIndex = 1;
            foreach (var dirPath in dirPaths) {
                var dirName = dirPath.FileName;
                foreach (var filePath in Directory.EnumerateFiles(dirPath, "coverage.*").Select(FilePath.New).ToArray()) {
                    var newFilePath = testOutputPath & $"{dirIndex}-{filePath.FileName}";
                    Console.WriteLine($"Moving: {filePath} -> {newFilePath}");
                    File.Move(filePath, newFilePath, true);
                }
                DeleteDir(dirPath);
                dirIndex++;
            }
        }

        private static FilePath? FindNearest(FilePath basePath, string fileName)
        {
            var path = basePath.ToAbsolute();
            while (true) {
                if (File.Exists(path & fileName))
                    return path & fileName;
                var nextPath = (path & "..").FullPath;
                if (nextPath == path)
                    return null;
                path = nextPath;
            }
        }


        private static FilePath? TryFindDotNetExePath()
        {
            var dotnetExe = "dotnet";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                dotnetExe += ".exe";

            var mainModulePath = FilePath.New(Process.GetCurrentProcess().MainModule?.FileName);
            if (mainModulePath.Value != "" && mainModulePath.FileName.Value.Equals(dotnetExe, StringComparison.OrdinalIgnoreCase))
                return mainModulePath;

            var dotnetRoot = FilePath.New(Environment.GetEnvironmentVariable("DOTNET_ROOT"));
            if (dotnetRoot.Value != "")
                return dotnetRoot & dotnetExe;

            return FindInPath(dotnetExe);
        }

        private static FilePath? FindInPath(string fileName)
        {
            var paths = Environment.GetEnvironmentVariable("PATH");
            if (paths == null)
                return null;

            foreach (var path in paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) {
                var fullPath = FilePath.New(path) & fileName;
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private static bool CreateDir(FilePath path, bool failOnError = false)
        {
            if (Directory.Exists(path))
                return true;
            Console.WriteLine($"Creating Directory: {path}");
            try {
                Directory.CreateDirectory(path);
                return true;
            }
            catch {
                if (failOnError)
                    throw;
                return false;
            }
        }

        private static bool DeleteDir(FilePath path, bool failOnError = false)
        {
            if (!Directory.Exists(path))
                return true;
            Console.WriteLine($"Deleting Directory: {path}");
            try {
                Directory.Delete(path, true);
                return true;
            }
            catch {
                if (failOnError)
                    throw;
                return false;
            }
        }
    }
}
