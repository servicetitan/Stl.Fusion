using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestEase;

namespace Stl.Samples.Blazor.Server.Services
{
    public interface IForismaticClient
    {
        [Get("?method=getQuote&format=json")]
        Task<JObject> GetQuoteAsync(
            [Query("lang")] string language = "en",
            CancellationToken cancellationToken = default);
    }
}
