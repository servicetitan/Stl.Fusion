using System.Threading.Tasks;

namespace Stl.Security
{
    public class PrefixScopedKeyVault : IKeyVault
    {
        protected IKeyVault KeyVault { get; }
        protected string Prefix { get; }
        public bool IsReadOnly => KeyVault.IsReadOnly;

        public PrefixScopedKeyVault(IKeyVault keyVault, string prefix)
        {
            KeyVault = keyVault;
            Prefix = prefix;
        }

        public string? TryGetSecret(string key) 
            => KeyVault.TryGetSecret(Prefix + key); 
        public ValueTask<string?> TryGetSecretAsync(string key)
            => KeyVault.TryGetSecretAsync(Prefix + key); 

        public void SetSecret(string key, string secret)
            => KeyVault.SetSecret(Prefix + key, secret);
        public ValueTask SetSecretAsync(string key, string secret)
            => KeyVault.SetSecretAsync(Prefix + key, secret);
    }
}
