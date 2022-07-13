using Stl.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class ComputeMethodFunction<T> : ComputeMethodFunctionBase<T>
{
    public ComputeMethodFunction(
        ComputeMethodDef methodDef,
        IServiceProvider services,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, services, versionGenerator)
    {
        if (methodDef.ComputedOptions.IsAsyncComputed)
            throw Errors.InternalError(
                $"This type can't be used with {nameof(Fusion.ComputedOptions)}.{nameof(Fusion.ComputedOptions.IsAsyncComputed)} == true option.");
    }

    protected override IComputed<T> CreateComputed(ComputeMethodInput input, LTag tag)
        => new Computed<T>(ComputedOptions, input, tag);
}
