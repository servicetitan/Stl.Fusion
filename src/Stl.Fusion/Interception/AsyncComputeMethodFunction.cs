using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class AsyncComputeMethodFunction<T> : ComputeMethodFunctionBase<T>
{
    public AsyncComputeMethodFunction(
        ComputeMethodDef method,
        VersionGenerator<LTag> versionGenerator,
        IServiceProvider services,
        ILogger<ComputeMethodFunction<T>>? log = null)
        : base(method, versionGenerator, services, log)
    {
        if (!method.Options.IsAsyncComputed)
            throw Stl.Internal.Errors.InternalError(
                $"This type shouldn't be used with {nameof(ComputedOptions)}.{nameof(ComputedOptions.IsAsyncComputed)} == false option.");
    }

    public override async Task<T> InvokeAndStrip(
        ComputeMethodInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;
        ResultBox<T>? output;

        // Read-Lock-RetryRead-Compute-Store pattern

        var computed = TryGetExisting(input);
        if (computed != null) {
            output = await computed.TryUseExisting(context, usedBy, cancellationToken)
                .ConfigureAwait(false);
            if (output != null)
                return output.Value;
        }

        using var @lock = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

        computed = TryGetExisting(input);
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
        => new SwappingComputed<T>(Options, input, tag);

    protected new IAsyncComputed<T>? TryGetExisting(ComputeMethodInput input)
    {
        var computed = ComputedRegistry.Instance.TryGet(input);
        return computed as IAsyncComputed<T>;
    }
}
