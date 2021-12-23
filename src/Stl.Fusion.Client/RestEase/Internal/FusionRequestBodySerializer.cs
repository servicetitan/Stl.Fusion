using System.Net.Http.Headers;
using RestEase;

namespace Stl.Fusion.Client.RestEase.Internal;

public class FusionRequestBodySerializer : RequestBodySerializer
{
    public ITextSerializer Serializer { get; init; } = SystemJsonSerializer.Default;
    public string ContentType { get; init; } = "application/json";

    public override HttpContent? SerializeBody<T>(T body, RequestBodySerializerInfo info)
    {
        if (body == null)
            return null;

        var content = new StringContent(Serializer.Write<T>(body));
        if (content.Headers.ContentType == null)
            content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
        else
            content.Headers.ContentType.MediaType = ContentType;
        return content;
    }
}
