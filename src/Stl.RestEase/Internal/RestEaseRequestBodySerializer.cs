using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using RestEase;
using Stl.Internal;

namespace Stl.RestEase.Internal;

public class RestEaseRequestBodySerializer : RequestBodySerializer
{
    public ITextSerializer Serializer { get; init; } = SystemJsonSerializer.Default;
    public string ContentType { get; init; } = "application/json";

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override HttpContent? SerializeBody<T>(T body, RequestBodySerializerInfo info)
#pragma warning restore IL2046
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
