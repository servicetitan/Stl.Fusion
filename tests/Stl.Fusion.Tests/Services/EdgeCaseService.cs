using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    [RegisterComputeService(typeof(IEdgeCaseService), Scope = ServiceScope.Services)]
    public class EdgeCaseService : IEdgeCaseService
    {
        public IMutableState<string> SuffixState { get; }

        public EdgeCaseService(IStateFactory stateFactory)
            => SuffixState = stateFactory.NewMutable<string>();

        public Task<string> GetSuffix(CancellationToken cancellationToken = default)
            => Task.FromResult(SuffixState.Value);

        public Task SetSuffix(string suffix, CancellationToken cancellationToken = default)
        {
            SuffixState.Value = suffix;
            return Task.CompletedTask;
        }

        public virtual async Task<string> ThrowIfContainsError(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + await SuffixState.Use(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<string> ThrowIfContainsErrorRewriteErrors(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + await SuffixState.Use(cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> ThrowIfContainsErrorNonCompute(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + SuffixState.Value;
        }
    }
}
