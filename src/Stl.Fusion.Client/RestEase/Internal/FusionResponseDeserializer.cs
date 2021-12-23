using System.Net;
using RestEase;

namespace Stl.Fusion.Client.RestEase.Internal;

public class FusionResponseDeserializer : ResponseDeserializer
{
    public ITextSerializer Serializer { get; init; } = SystemJsonSerializer.Default;

    public override T Deserialize<T>(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
        => response.StatusCode == HttpStatusCode.NoContent
            ? default!
            : Serializer.Read<T>(content!);
}
