using System.Net;
using RestEase;

namespace Stl.Fusion.Client.RestEase.Internal;

public class FusionResponseDeserializer : ResponseDeserializer
{
    public ITextReader Reader { get; init; } = SystemJsonSerializer.Default.Reader;

    public override T Deserialize<T>(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
        => response.StatusCode == HttpStatusCode.NoContent
            ? default!
            : Reader.Read<T>(content!);
}
