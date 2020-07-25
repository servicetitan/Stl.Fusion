using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stl.Async;

namespace Stl.Security
{
    public class ConfigurationKeyVault : IKeyVault
    {
        protected IConfiguration Vault { get; }

        public ConfigurationKeyVault(IConfiguration vault) => Vault = vault;

        public string? TryGetSecret(string key)
            => Vault[key];
        public ValueTask<string?> TryGetSecretAsync(string key)
            => ValueTaskEx.FromResult(TryGetSecret(key));
    }
}
