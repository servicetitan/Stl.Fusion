using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Fusion.Caching;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : CachingFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;
        protected readonly IServiceProvider Services;

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
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var method = Method;
            var computed = new Computed<T>(method.Options, input, tag);
            try {
                using var _ = Computed.ChangeCurrent(computed);
                if (IsCachingEnabled) {
                    var maybeCachedOutput = await GetCachedOutputAsync(input, cancellationToken)
                        .ConfigureAwait(false);
                    if (maybeCachedOutput.IsSome(out var cachedOutput)) {
                        computed.TrySetOutput(cachedOutput);
                        SetReturnValue(input, cachedOutput);
                        return computed;
                    }
                }
                var resultTask = input.InvokeOriginalFunction(cancellationToken);
                if (method.ReturnsValueTask) {
                    var task = (ValueTask<T>) resultTask;
                    var value = await task.ConfigureAwait(false);
                    computed.TrySetOutput(value!);
                }
                else {
                    var task = (Task<T>) resultTask;
                    var value = await task.ConfigureAwait(false);
                    computed.TrySetOutput(value!);
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                computed.TrySetOutput(Result.Error<T>(e));
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            return computed;
        }

        public override async ValueTask<Option<Result<T>>> GetCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default)
        {
            var cache = (ICache) Services.GetRequiredService(CachingOptions.CacheType);
            var maybeResult = await cache.GetAsync(input, cancellationToken).ConfigureAwait(false);
            return maybeResult.IsSome(out var r) ? r.Cast<T>() : Option<Result<T>>.None;
        }
    }
}
