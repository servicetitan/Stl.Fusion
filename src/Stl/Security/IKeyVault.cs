using System.Threading.Tasks;

namespace Stl.Security
{
    public interface IKeyVault
    {
        string? TryGetSecret(string key);
        ValueTask<string?> TryGetSecretAsync(string key);
    }
}
