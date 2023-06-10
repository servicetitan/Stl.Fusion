using System.Net;
using Newtonsoft.Json.Linq;
using Stl.Fusion.Client.Internal;

namespace Stl.RestEase.Internal;

public class RestEaseHttpMessageHandler : DelegatingHandler, IHasServices
{
    public IServiceProvider Services { get; }

    public RestEaseHttpMessageHandler(IServiceProvider services)
        => Services = services;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.InternalServerError) {
            // [JsonifyErrors] responds with this status code
            var error = await DeserializeError(response).ConfigureAwait(false);
            throw error;
        }
        return response;
    }

    private async Task<Exception> DeserializeError(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType;
        if (!StringComparer.Ordinal.Equals(contentType?.MediaType ?? "", "application/json"))
            return new RemoteException(content);

        try {
            var serializer = TypeDecoratingSerializer.Default;
            return serializer.Read<ExceptionInfo>(content).ToException()
                ?? Errors.UnknownServerSideError();
        }
        catch (Exception) {
            try {
                var jError = JObject.Parse(content);
                var message = jError[nameof(Exception.Message)]?.Value<string>();
                return message.IsNullOrEmpty()
                    ? Errors.UnknownServerSideError()
                    : new RemoteException(message!);
            }
            catch (Exception) {
                return Errors.UnknownServerSideError();
            }
        }
    }
}
