using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestEase;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Client.Internal;
using Stl.Serialization;

namespace Stl.Fusion.Client.RestEase.Internal
{
    public class FusionHttpMessageHandler : DelegatingHandler, IHasServiceProvider
    {
        public IServiceProvider ServiceProvider { get; }

        public FusionHttpMessageHandler(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var headers = response.Headers;
            headers.TryGetValues(FusionHeaders.Publication, out var values);
            var publicationStateInfoJson = values?.FirstOrDefault();
            if (!string.IsNullOrEmpty(publicationStateInfoJson)) {
                var psi = JsonConvert.DeserializeObject<PublicationStateInfo>(publicationStateInfoJson);
                if (response.StatusCode == HttpStatusCode.InternalServerError) {
                    var error = await DeserializeError(response).ConfigureAwait(false);
                    psi = new PublicationStateInfo<object>(psi, Result.Error<object>(error));
                    PublicationStateInfoCapture.TryCapture(psi);
                    throw error;
                }
                PublicationStateInfoCapture.TryCapture(psi);
            }
            else {
                if (response.StatusCode == HttpStatusCode.InternalServerError)
                    throw await DeserializeError(response).ConfigureAwait(false);
            }
            return response;
        }

        private async Task<Exception> DeserializeError(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType;
            if (contentType.MediaType != "application/json")
                return new ServiceException(content);

            var serializer = new JsonNetSerializer(JsonNetSerializer.DefaultSettings);
            try {
                return serializer.Deserialize<Exception>(content);
            }
            catch (Exception) {
                try {
                    var jError = JObject.Parse(content);
                    var message = jError[nameof(ServiceException.Message)]?.Value<string>();
                    return string.IsNullOrEmpty(message)
                        ? Errors.UnknownServerSideError()
                        : new ServiceException(message);
                }
                catch (Exception) {
                    return Errors.UnknownServerSideError();
                }
            }
        }
    }
}
