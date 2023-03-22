namespace Stl.Fusion.Interception;

public interface IComputedOptionsProvider
{
    ComputedOptions? GetComputedOptions(MethodInfo methodInfo, Type proxyType);
    ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo, Type proxyType);
}

public record ComputedOptionsProvider : IComputedOptionsProvider
{
    public virtual ComputedOptions? GetComputedOptions(MethodInfo methodInfo, Type proxyType)
    {
        var attribute = GetComputeMethodAttribute(methodInfo, proxyType);
        if (attribute == null)
            return null;

        var defaultOptions = typeof(IReplicaService).IsAssignableFrom(proxyType)
            ? ComputedOptions.ReplicaDefault
            : ComputedOptions.Default;
        return ComputedOptions.FromAttribute(defaultOptions, attribute);
    }

    public virtual ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo, Type proxyType)
        => methodInfo.GetAttribute<ComputeMethodAttribute>(true, true);
}
