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
    public class ComputeServiceFunction<T> : InterceptedFunctionBase<T>, ICachingFunction<InterceptedInput, T>
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
            var options = method.Options;
            var cacheOptions = options.CacheOptions;
            var computed = new Computed<T>(options, input, tag);
            try {
                using var _ = Computed.ChangeCurrent(computed);
                if (cacheOptions.IsCachingEnabled) {
                    var maybeCachedOutput = await GetCachedOutputAsync(input, cacheOptions, cancellationToken)
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

        public async ValueTask<Option<Result<T>>> GetCachedOutputAsync(
            InterceptedInput input, CacheOptions cacheOptions,
            CancellationToken cancellationToken = default)
        {
            var cache = (ICache) Services.GetRequiredService(cacheOptions.CacheType);
            var maybeResult = await cache.GetAsync(input, cancellationToken).ConfigureAwait(false);
            return maybeResult.IsSome(out var r) ? r.Cast<T>() : Option<Result<T>>.None;
        }

        // Private methods

        private static void SetReturnValue(InterceptedInput input, Result<T> output)
        {
            if (input.Method.ReturnsValueTask)
                input.Invocation.ReturnValue =
                    output.IsValue(out var v)
                        ? ValueTaskEx.FromResult(v)
                        : ValueTaskEx.FromException<T>(output.Error!);
            else
                input.Invocation.ReturnValue =
                    output.IsValue(out var v)
                        ? Task.FromResult(v)
                        : Task.FromException<T>(output.Error!);
        }
    }
}
