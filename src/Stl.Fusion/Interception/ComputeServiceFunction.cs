using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Caching;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : CachingFunctionBase<T>
    {
        private readonly Lazy<ICache<InterceptedInput>> _cacheLazy;
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;
        protected readonly IServiceProvider Services;
        protected ICache<InterceptedInput> Cache => _cacheLazy.Value;

        public ComputeServiceFunction(
            InterceptedMethod method,
            Generator<LTag> versionGenerator,
            IServiceProvider services,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(method)
        {
            Log = log ??= NullLogger<ComputeServiceFunction<T>>.Instance;
            VersionGenerator = versionGenerator;
            Services = services;
            _cacheLazy = new Lazy<ICache<InterceptedInput>>(
                () => (ICache<InterceptedInput>) Services.GetRequiredService(CachingOptions.CacheType));
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var method = Method;

            CachingComputed<T>? cachingComputed;
            Computed<T> computed;
            Result<T> output;
            if (IsCachingEnabled)
                computed = cachingComputed = new CachingComputed<T>(method.Options, input, tag);
            else {
                computed = new Computed<T>(method.Options, input, tag);
                cachingComputed = null;
            }
            try {
                using var _ = Computed.ChangeCurrent(computed);
                if (cachingComputed != null) {
                    var cachedOutput = await GetCachedOutputAsync(input, cancellationToken)
                        .ConfigureAwait(false);
                    if (cachedOutput != null) {
                        cachingComputed.TrySetOutput(cachedOutput, true);
                        SetReturnValue(input, cachedOutput.ToResult());
                        return computed;
                    }
                }
                var result = input.InvokeOriginalFunction(cancellationToken);
                if (method.ReturnsValueTask) {
                    output = await ((ValueTask<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
                }
                else {
                    output = await ((Task<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                output = Result.Error<T>(e);
                computed.TrySetOutput(output);
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            if (cachingComputed != null)
                await SetCachedOutputAsync(input, new ResultBox<T>(output), cancellationToken)
                    .ConfigureAwait(false);
            return computed;
        }

        public override async Task<ResultBox<T>?> GetCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default)
        {
            var resultOpt = await Cache.GetAsync(input, cancellationToken).ConfigureAwait(false);
            return resultOpt.IsSome(out var result) ? result as ResultBox<T> : null;
        }

        public override ValueTask SetCachedOutputAsync(InterceptedInput input, ResultBox<T> output, CancellationToken cancellationToken = default)
            => Cache.SetAsync(input, output, CachingOptions.ExpirationTime, cancellationToken);

        public override ValueTask RemoveCachedOutputAsync(InterceptedInput input, CancellationToken cancellationToken = default)
            => Cache.RemoveAsync(input, cancellationToken);
    }
}
