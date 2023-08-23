using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class ComputeMethodFunction<T>(
    ComputeMethodDef methodDef,
    IServiceProvider services,
    VersionGenerator<LTag> versionGenerator
    ) : ComputeMethodFunctionBase<T>(methodDef, services, versionGenerator)
{
    protected override Computed<T> CreateComputed(ComputeMethodInput input, LTag tag)
        => new ComputeMethodComputed<T>(ComputedOptions, input, tag);
}
