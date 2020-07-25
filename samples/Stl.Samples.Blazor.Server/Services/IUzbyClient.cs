using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace Stl.Samples.Blazor.Server.Services
{
    public interface IUzbyClient
    {
        [Get("")]
        Task<string> GetNameAsync(
            [Query("min")] int minLength = 2,
            [Query("max")] int maxLength = 8,
            CancellationToken cancellationToken = default);
    }
}
