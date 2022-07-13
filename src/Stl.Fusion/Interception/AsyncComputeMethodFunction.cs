using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class AsyncComputeMethodFunction<T> : ComputeMethodFunctionBase<T>
{
    public AsyncComputeMethodFunction(
        ComputeMethodDef methodDef,
        IServiceProvider services,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, services, versionGenerator)
    {
        if (!methodDef.ComputedOptions.IsAsyncComputed)
            throw Stl.Internal.Errors.InternalError(
                $"This type shouldn't be used with {nameof(Fusion.ComputedOptions)}.{nameof(Fusion.ComputedOptions.IsAsyncComputed)} == false option.");
    }

    public override async Task<T> InvokeAndStrip(
        ComputeMethodInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;
        ResultBox<T>? output;

        // Read-Lock-RetryRead-Compute-Store pattern

        var computed = GetExisting(input);
        if (computed != null) {
            output = await computed.TryUseExisting(context, usedBy, cancellationToken)
                .ConfigureAwait(false);
            if (output != null)
                return output.Value;
        }

        using var @lock = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

        computed = GetExisting(input);
        if (computed != null) {
            output = await computed.TryUseExisting(context, usedBy, cancellationToken)
                .ConfigureAwait(false);
            if (output != null)
                return output.Value;
        }

        computed = (IAsyncComputed<T>) await Compute(input, computed, cancellationToken)
            .ConfigureAwait(false);
        var rOutput = computed.Output; // RenewTimeouts isn't called yet, so it's ok
        computed.UseNew(context, usedBy);
        return rOutput!.Value;
    }

    protected override IComputed<T> CreateComputed(ComputeMethodInput input, LTag tag)
        => new SwappingComputed<T>(ComputedOptions, input, tag);

    protected new IAsyncComputed<T>? GetExisting(ComputeMethodInput input)
    {
        var computed = ComputedRegistry.Instance.Get(input);
        return computed as IAsyncComputed<T>;
    }
}
