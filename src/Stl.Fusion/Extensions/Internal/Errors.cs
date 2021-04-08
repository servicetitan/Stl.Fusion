using System;

namespace Stl.Fusion.Extensions.Internal
{
    public static class Errors
    {
        public static Exception KeyViolatesIsolatedKeyValueStoreConstraints()
            => throw new InvalidOperationException("Key violates isolated key-value store constraints.");
    }
}
