namespace Stl.Fusion.Extensions.Internal;

public static class Errors
{
    public static Exception KeyViolatesSandboxedKeyValueStoreConstraints()
        => throw new InvalidOperationException("Key violates sandboxed key-value store constraints.");
}
