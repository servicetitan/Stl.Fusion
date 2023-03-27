namespace Stl.Fusion.Interception;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ComputedOptionsProvider
{
    public virtual ComputedOptions? GetComputedOptions(Type type, MethodInfo method)
        => ComputedOptions.Get(type, method);
}
