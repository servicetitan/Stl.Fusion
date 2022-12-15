namespace Stl.Conversion;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConverterProvider Converters(this IServiceProvider services)
        => services.GetService<IConverterProvider>() ?? ConverterProvider.Default;
}
