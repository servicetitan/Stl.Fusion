using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public abstract class ComputeServiceFunctionBase<T> : InterceptedFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;

        public ComputeServiceFunctionBase(
            InterceptedMethod method,
            Generator<LTag> versionGenerator,
            IServiceProvider serviceProvider,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(method, serviceProvider)
        {
            Log = log ??= NullLogger<ComputeServiceFunction<T>>.Instance;
            VersionGenerator = versionGenerator;
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var computed = CreateComputed(input, tag);
            try {
                using var _ = Computed.ChangeCurrent(computed);
                var result = input.InvokeOriginalFunction(cancellationToken);
                if (input.Method.ReturnsValueTask) {
                    var output = await ((ValueTask<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
                }
                else {
                    var output = await ((Task<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
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

        protected abstract IComputed<T> CreateComputed(InterceptedInput input, LTag tag);
    }
}
