using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stl.Security
{
    public static class KeyVaultEx
    {
        public static string GetSecret(this IKeyVault keyVault, string key)
            => keyVault.TryGetSecret(key) ?? throw new KeyNotFoundException();
        public static async ValueTask<string> GetSecretAsync(this IKeyVault keyVault, string key)
            => (await keyVault.TryGetSecretAsync(key)) ?? throw new KeyNotFoundException();

        public static IKeyVault GetSection(this IKeyVault keyVault, string prefix, string delimiter = ":")
            => keyVault.WithRawPrefix(prefix + delimiter);
        public static IKeyVault WithRawPrefix(this IKeyVault keyVault, string rawPrefix)
            => new PrefixScopedKeyVault(keyVault, rawPrefix);
    }
}
