using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Client.Internal;

namespace Stl.Fusion.Client.RestEase.Internal;

public class FusionHttpMessageHandler : DelegatingHandler, IHasServices
{
    public IServiceProvider Services { get; }

    public FusionHttpMessageHandler(IServiceProvider services)
        => Services = services;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var psiCapture = PublicationStateInfoCapture.Current;
        if (psiCapture != null)
            // Publication request -> we need to add header
            request.Headers.Add(FusionHeaders.RequestPublication, "1");

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (psiCapture == null) {
            // Regular request
            if (response.StatusCode == HttpStatusCode.InternalServerError)
                throw await DeserializeError(response).ConfigureAwait(false);
            return response;
        }

        // Publication request
        var headers = response.Headers;
        headers.TryGetValues(FusionHeaders.Publication, out var values);
        var psiJson = values?.FirstOrDefault();
        if (string.IsNullOrEmpty(psiJson))
            throw Fusion.Internal.Errors.NoPublicationStateInfoCaptured();

        var psi = JsonConvert.DeserializeObject<PublicationStateInfo>(psiJson!)!;
        if (response.StatusCode == HttpStatusCode.InternalServerError) {
            var error = await DeserializeError(response).ConfigureAwait(false);
            psi = new PublicationStateInfo<object>(psi, Result.Error<object>(error));
            psiCapture.Capture(psi);
            throw error;
        }
        psiCapture.Capture(psi);
        return response;
    }

    private async Task<Exception> DeserializeError(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType;
        if (contentType?.MediaType != "application/json")
            return new ServiceException(content);

        try {
            var serializer = TypeDecoratingSerializer.Default;
            return serializer.Reader.Read<ExceptionInfo>(content).ToException()!;
        }
        catch (Exception) {
            try {
                var jError = JObject.Parse(content);
                var message = jError[nameof(ServiceException.Message)]?.Value<string>();
                return string.IsNullOrEmpty(message)
                    ? Errors.UnknownServerSideError()
                    : new ServiceException(message!);
            }
            catch (Exception) {
                return Errors.UnknownServerSideError();
            }
        }
    }
}
