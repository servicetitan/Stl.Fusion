namespace Stl.Fusion.Client
{
    public static class FusionHeaders
    {
        public const string CommonPrefix = "X-Fusion-"; 
        public const string Publish = "X-Fusion-Publish"; // Must be const string to be used in attribute 
        public static readonly string PublisherId = $"{CommonPrefix}PublisherId"; 
        public static readonly string PublicationId = $"{CommonPrefix}PublicationId";
        public static readonly string LTag = $"{CommonPrefix}LTag";
        public static readonly string IsConsistent = $"{CommonPrefix}IsConsistent";
    }
}
