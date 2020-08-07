namespace Stl.Fusion.Client
{
    public static class FusionHeaders
    {
        public const string CommonPrefix = "X-Fusion-";
        public const string RequestPublication = "X-Fusion-Publish"; // Must be const string to be used in attribute
        public static readonly string PublisherId = $"{CommonPrefix}{nameof(PublisherId)}";
        public static readonly string PublicationId = $"{CommonPrefix}{nameof(PublicationId)}";
        public static readonly string Version = $"{CommonPrefix}{nameof(Version)}";
        public static readonly string IsConsistent = $"{CommonPrefix}{nameof(IsConsistent)}";
    }
}
