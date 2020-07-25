using System;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Security
{
    public sealed class NullKeyVault : IKeyVault
    {
        public static readonly NullKeyVault Instance = new NullKeyVault();

        public string? TryGetSecret(string key)
            => null;
        public ValueTask<string?> TryGetSecretAsync(string key)
            => ValueTaskEx.FromResult((string?) null);
    }
}
