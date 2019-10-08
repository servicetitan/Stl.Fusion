using System.Threading.Tasks;

namespace Stl.Security 
{
    public interface IKeyVault
    {
        bool IsReadOnly { get; }

        string? TryGetSecret(string key);
        ValueTask<string?> TryGetSecretAsync(string key);

        void SetSecret(string key, string secret);
        ValueTask SetSecretAsync(string key, string secret);
    }
}
