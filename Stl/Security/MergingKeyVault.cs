using System;
using System.Threading.Tasks;

namespace Stl.Security
{
    public class MergingKeyVault : IKeyVault
    {
        public bool IsReadOnly => true;
        public IKeyVault PrimaryVault { get; }
        public IKeyVault SecondaryVault { get; }

        public MergingKeyVault(IKeyVault primaryVault, IKeyVault secondaryVault)
        {
            PrimaryVault = primaryVault;
            SecondaryVault = secondaryVault;
        }

        public string? TryGetSecret(string key) 
            => PrimaryVault.TryGetSecret(key) ?? SecondaryVault.TryGetSecret(key); 
        public async ValueTask<string?> TryGetSecretAsync(string key) 
            => (await PrimaryVault.TryGetSecretAsync(key).ConfigureAwait(false)) 
                ?? (await SecondaryVault.TryGetSecretAsync(key).ConfigureAwait(false)); 

        public void SetSecret(string key, string secret)
            => throw new NotSupportedException();
        public ValueTask SetSecretAsync(string key, string secret)
            => throw new NotSupportedException();
    }
}
