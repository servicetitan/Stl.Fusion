using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Internal;

namespace Stl.Fusion.Client.RestEase.Internal
{
    public class FusionHttpMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var headers = response.Headers;
            if (headers.TryGetValues(FusionHeaders.Publication, out var values)) {
                var header = values.FirstOrDefault();
                if (!string.IsNullOrEmpty(header)) {
                    var psi = JsonConvert.DeserializeObject<PublicationStateInfo>(header);
                    if (response.StatusCode == HttpStatusCode.InternalServerError) {
                        // Providing extended error info
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var error = new ApiException(request, response, content);
                        psi = new PublicationStateInfo<object>(psi, Result.Error<object>(error));
                    }
                    PublicationStateInfoCapture.TryCapture(psi);
                }
            }
            return response;
        }
    }
}
