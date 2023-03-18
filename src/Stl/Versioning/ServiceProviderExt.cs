namespace Stl.Versioning;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionGenerator<TVersion> VersionGenerator<TVersion>(this IServiceProvider services)
        where TVersion : notnull
        => services.GetRequiredService<VersionGenerator<TVersion>>();
}
