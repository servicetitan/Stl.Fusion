using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion;
using static System.Console;

namespace Samples.HelloWorld
{
    public class IncrementalBuilder
    {
        private readonly ConcurrentDictionary<string, Project> _projects = new();
        private readonly ConcurrentDictionary<string, long> _versions = new();

        [ComputeMethod]
        public virtual async Task<ProjectBuildResult> GetOrBuildAsync(string projectId, CancellationToken cancellationToken = default)
        {
            WriteLine($"> Building: {projectId}");
            // Get project & new version of its output
            var project = _projects[projectId];
            var version = _versions.AddOrUpdate(projectId, id => 1, (id, version) => version + 1);
            // Build dependencies
            await Task.WhenAll(project.DependsOn.Select(
                // IMPORTANT: Noticed recursive GetOrBuildAsync call below?
                // Such calls - i.e. calls made inside [ComputeMethod]-s to
                // other [ComputeMethod]-s - is all Fusion needs to know that
                // A (currently produced output) depends on B (the output of
                // whatever is called).
                // Note it's also totally fine to run such calls concurrently.
                dependencyId => GetOrBuildAsync(dependencyId, cancellationToken)));
            // Simulate build
            await Task.Delay(100);

            var result = new ProjectBuildResult() {
                Project = project,
                Version = version,
                Artifacts = $"{projectId}.lib",
            };
            WriteLine($"< {projectId}: {result.Artifacts}, v{result.Version}");
            return result;
        }

        public Task AddOrUpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            _projects.AddOrUpdate(project.Id, id => project, (id, _) => project);
            InvalidateGetOrBuildResult(project.Id);
            return Task.CompletedTask;
        }

        public void InvalidateGetOrBuildResult(string projectId)
        {
            // WriteLine($"Invalidating build results for: {projectId}");
            using var _ = Computed.Invalidate();
            // Invalidation call to [ComputeMethod] always completes synchronously, so...
            GetOrBuildAsync(projectId, default).Ignore(); // Ignore() call is here just to suppress warning
        }
    }
}
