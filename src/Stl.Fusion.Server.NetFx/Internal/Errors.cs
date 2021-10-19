using Microsoft.Extensions.Primitives;

namespace Stl.Fusion.Server.Internal;

public static class Errors
{
    public static Exception AlreadyPublished()
        => new InvalidOperationException(
            "Only one publication can be published for a given HTTP request.");

    public static Exception UnsupportedWebApiEndpoint()
        => new InvalidOperationException("This method can't be invoked via Web API.");

    public static Exception UnknownCharset(StringSegment charset, Exception innerException)
        => new InvalidOperationException(
            $"Unable to read the request as JSON because the request content type charset '{charset}' is not a known encoding.",
            innerException);

    public static Exception UnknownContentType(string contentType)
        => new InvalidOperationException(
            $"Unable to read the request as JSON because the request content type '{contentType}' is not a known JSON content type.");
}
