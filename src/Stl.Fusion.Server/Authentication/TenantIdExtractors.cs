using System.Globalization;
using Microsoft.AspNetCore.Http;
using Stl.Internal;

namespace Stl.Fusion.Server.Authentication;

public static class TenantIdExtractors
{
    public const string DefaultCookieName = "Fusion.TenantId";
    public const string DefaultHeaderName = "X-Fusion-TenantId";

    public static readonly Func<HttpContext, Symbol> None = _ => Symbol.Empty;

    public static Func<HttpContext, Symbol> Constant(Symbol tenantId)
        => _ => tenantId;

    public static Func<HttpContext, Symbol> FromSubdomain(string subdomainSuffix = ".", string idFormat = "{0}")
        => httpContext => {
            var host = httpContext.Request.Host.Host;
            var suffixIndex = host.IndexOf(subdomainSuffix, StringComparison.Ordinal);
            if (suffixIndex <= 0)
                return default;
            var subdomain = host[..suffixIndex];
            return string.Format(CultureInfo.InvariantCulture, idFormat, subdomain);
        };

    public static Func<HttpContext, Symbol> FromPort(Range<int> portRange, string idFormat = "tenant{0}")
        => httpContext => {
            var port = httpContext.Connection.LocalPort;
            if (!portRange.Contains(port))
                return default;
            var tenantIndex = port - portRange.Start;
            return string.Format(CultureInfo.InvariantCulture, idFormat, tenantIndex);
        };

    public static Func<HttpContext, Symbol> FromCookie(string cookieName = DefaultCookieName)
        => httpContext => {
            var cookies = httpContext.Request.Cookies;
            cookies.TryGetValue(cookieName, out var tenantId);
            return tenantId;
        };

    public static Func<HttpContext, Symbol> FromHeader(string headerName = DefaultHeaderName)
        => httpContext => {
            var cookies = httpContext.Request.Headers;
            cookies.TryGetValue(headerName, out var tenantId);
            return tenantId.LastOrDefault();
        };

    // Combinators

    public static Func<HttpContext, Symbol> Or(
        this Func<HttpContext, Symbol> extractor,
        Func<HttpContext, Symbol> alternativeExtractor)
        => httpContext => {
            var tenantId = extractor.Invoke(httpContext);
            if (!tenantId.IsEmpty)
                return tenantId;
            return alternativeExtractor.Invoke(httpContext);
        };

    public static Func<HttpContext, Symbol> WithValidator(
        this Func<HttpContext, Symbol> extractor,
        Func<Symbol, bool> validator)
        => httpContext => {
            var tenantId = extractor.Invoke(httpContext);
            if (tenantId.IsEmpty)
                return tenantId;
            if (!validator.Invoke(tenantId))
                throw Errors.TenantNotFound(tenantId);
            return tenantId;
        };

    public static Func<HttpContext, Symbol> WithMapper(
        this Func<HttpContext, Symbol> extractor,
        Func<Symbol, Symbol> mapper)
        => httpContext => {
            var tenantId = extractor.Invoke(httpContext);
            if (tenantId.IsEmpty)
                return tenantId;
            return mapper.Invoke(tenantId);
        };
}
