using System.Diagnostics.CodeAnalysis;
using System.Net;
using Newtonsoft.Json.Linq;
using Stl.Internal;

namespace Stl.RestEase.Internal;

public class RestEaseHttpMessageHandler(IServiceProvider services) : DelegatingHandler, IHasServices
{
    public IServiceProvider Services { get; } = services;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
#pragma warning restore IL2046
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.InternalServerError) {
            // [JsonifyErrors] responds with this status code
            var error = await DeserializeError(response).ConfigureAwait(false);
            throw error;
        }
        return response;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private static async Task<Exception> DeserializeError(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType;
        if (!StringComparer.Ordinal.Equals(contentType?.MediaType ?? "", "application/json"))
            return new RemoteException(content);

        try {
            var serializer = TypeDecoratingTextSerializer.Default;
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
