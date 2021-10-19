using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Stl.Fusion.Server.Internal;

public static class HttpRequestExt
{
    public const string JsonContentType = "application/json";
    public const string JsonContentTypeWithCharset = "application/json; charset=utf-8";

    public static bool HasJsonContentType(this HttpRequest request)
    {
        return request.HasJsonContentType(out _);
    }

    public static bool HasJsonContentType(this HttpRequest request, out StringSegment charset)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt)) {
            charset = StringSegment.Empty;
            return false;
        }

        // Matches application/json
        if (mt.MediaType.Equals(JsonContentType, StringComparison.OrdinalIgnoreCase)) {
            charset = mt.Charset;
            return true;
        }

        // Matches +json, e.g. application/ld+json
        if (mt.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase)) {
            charset = mt.Charset;
            return true;
        }

        charset = StringSegment.Empty;
        return false;
    }
}
