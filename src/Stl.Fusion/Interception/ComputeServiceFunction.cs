using System;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;
using Stl.Internal;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : ComputeServiceFunctionBase<T>
    {
        public ComputeServiceFunction(
            IServiceProvider serviceProvider,
            InterceptedMethod method,
            Generator<LTag> versionGenerator,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(serviceProvider, method, versionGenerator, log)
        {
            if (method.Options.IsAsyncComputed)
                throw Errors.InternalError(
                    $"This type can't be used with {nameof(ComputedOptions)}.{nameof(ComputedOptions.IsAsyncComputed)} == true option.");
        }

        protected override IComputed<T> CreateComputed(InterceptedInput input, LTag tag)
            => new Computed<T>(Options, input, tag);
    }
}
