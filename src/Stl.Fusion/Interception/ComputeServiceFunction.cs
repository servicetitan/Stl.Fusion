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
        private readonly Lazy<ICache<InterceptedInput, Result<object>>> _cacheLazy;
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;
        protected readonly IServiceProvider Services;
        protected ICache<InterceptedInput, Result<object>> Cache => _cacheLazy.Value;

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
            _cacheLazy = new Lazy<ICache<InterceptedInput, Result<object>>>(
                () => (ICache<InterceptedInput, Result<object>>) Services.GetRequiredService(CachingOptions.CacheType));
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var method = Method;

            CachingComputed<T>? cachingComputed;
            Computed<T> computed;
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
                    if (cachedOutput.IsSome(out var output)) {
                        cachingComputed.TrySetOutput(output, true);
                        SetReturnValue(input, output);
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
            if (cachingComputed != null)
                await SetCachedOutputAsync(input, Option.Some(computed.Output.Cast<object>()), cancellationToken)
                    .ConfigureAwait(false);
            return computed;
        }

        public override async ValueTask<Option<Result<T>>> GetCachedOutputAsync(
            InterceptedInput input, CancellationToken cancellationToken = default)
        {
            var resultOpt = await Cache.GetAsync(input, cancellationToken).ConfigureAwait(false);
            return resultOpt.IsSome(out var r) ? r.Cast<T>() : Option<Result<T>>.None;
        }

        public override ValueTask SetCachedOutputAsync(InterceptedInput input, Result<object> output, CancellationToken cancellationToken = default)
            => Cache.SetAsync(input, output, CachingOptions.ExpirationTime, cancellationToken);

        public override ValueTask RemoveCachedOutputAsync(InterceptedInput input, CancellationToken cancellationToken = default)
            => Cache.RemoveAsync(input, cancellationToken);
    }
}
