namespace Stl.Fusion.Interception;

public interface IComputedOptionsProvider
{
    ComputedOptions? GetComputedOptions(MethodInfo methodInfo, Type proxyType);
    ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo, Type proxyType);
    SwapAttribute? GetSwapAttribute(MethodInfo methodInfo, Type proxyType);
}

public record ComputedOptionsProvider : IComputedOptionsProvider
{
    public virtual ComputedOptions? GetComputedOptions(MethodInfo methodInfo, Type proxyType)
    {
        var attribute = GetComputeMethodAttribute(methodInfo, proxyType);
        if (attribute == null)
            return null;

        var swapAttribute = GetSwapAttribute(methodInfo, proxyType);
        var defaultOptions = typeof(IReplicaService).IsAssignableFrom(proxyType)
            ? ComputedOptions.ReplicaDefault
            : ComputedOptions.Default;
        return ComputedOptions.FromAttribute(defaultOptions, attribute, swapAttribute);
    }

    public virtual ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo, Type proxyType)
        => methodInfo.GetAttribute<ComputeMethodAttribute>(true, true);

    public virtual SwapAttribute? GetSwapAttribute(MethodInfo methodInfo, Type proxyType)
        => methodInfo.GetAttribute<SwapAttribute>(true, true);
}
