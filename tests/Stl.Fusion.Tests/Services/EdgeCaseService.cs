using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    [ComputeService(typeof(IEdgeCaseService))]
    public class EdgeCaseService : IEdgeCaseService
    {
        public IMutableState<string> SuffixState { get; }

        public EdgeCaseService(IStateFactory stateFactory)
            => SuffixState = stateFactory.NewMutable<string>();

        public Task<string> GetSuffixAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(SuffixState.Value);

        public Task SetSuffixAsync(string suffix, CancellationToken cancellationToken = default)
        {
            SuffixState.Value = suffix;
            return Task.CompletedTask;
        }

        public virtual async Task<string> ThrowIfContainsErrorAsync(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + await SuffixState.UseAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<string> ThrowIfContainsErrorRewriteErrorsAsync(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + await SuffixState.UseAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> ThrowIfContainsErrorNonComputeAsync(string source, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            return source.ToLowerInvariant().Contains("error")
                ? throw new ArgumentException("!")
                : source + SuffixState.Value;
        }
    }
}
