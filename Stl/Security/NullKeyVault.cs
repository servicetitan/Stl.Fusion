using System;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Security
{
    public sealed class NullKeyVault : IKeyVault
    {
        public static readonly NullKeyVault Instance = new NullKeyVault();

        public bool IsReadOnly => true;

        public string? TryGetSecret(string key) 
            => null;
        public ValueTask<string?> TryGetSecretAsync(string key) 
            => ValueTaskEx.New((string?) null);

        public void SetSecret(string key, string secret) 
            => throw new NotSupportedException();
        public ValueTask SetSecretAsync(string key, string secret)
            => throw new NotSupportedException();
    }
}
