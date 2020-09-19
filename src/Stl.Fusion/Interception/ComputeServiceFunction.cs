using System;
using Microsoft.Extensions.Logging;
using Stl.Generators;
using Stl.Internal;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : ComputeServiceFunctionBase<T>
    {
        public ComputeServiceFunction(
            InterceptedMethodDescriptor method,
            Generator<LTag> versionGenerator,
            IServiceProvider serviceProvider,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(method, versionGenerator, serviceProvider, log)
        {
            if (method.Options.IsAsyncComputed)
                throw Errors.InternalError(
                    $"This type can't be used with {nameof(ComputedOptions)}.{nameof(ComputedOptions.IsAsyncComputed)} == true option.");
        }

        protected override IComputed<T> CreateComputed(InterceptedInput input, LTag tag)
            => new Computed<T>(Options, input, tag);
    }
}
