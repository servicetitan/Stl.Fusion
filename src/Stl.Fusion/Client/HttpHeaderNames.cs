namespace Stl.Fusion.Client
{
    public static class HttpHeaderNames
    {
        public static readonly string CommonPrefix = "X-Fusion-"; 
        public static readonly string PublisherId = $"{CommonPrefix}PublisherId"; 
        public static readonly string PublicationId = $"{CommonPrefix}PublicationId";
        public static readonly string LTag = $"{CommonPrefix}LTag";
        public static readonly string IsConsistent = $"{CommonPrefix}IsConsistent";
    }
}
