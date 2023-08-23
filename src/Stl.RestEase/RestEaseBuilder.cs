using System.Globalization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using RestEase;
using Stl.RestEase.Internal;

namespace Stl.RestEase;

public readonly struct RestEaseBuilder
{
    private class AddedTag;
    private static readonly ServiceDescriptor AddedTagDescriptor =
        new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }

    internal RestEaseBuilder(
        IServiceCollection services,
        Action<RestEaseBuilder>? configure)
    {
        Services = services;
        if (services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);

        // FusionHttpMessageHandler (handles Fusion headers)
        services.AddHttpClient();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IHttpMessageHandlerBuilderFilter,
            RestEaseHttpMessageHandlerBuilderFilter>());

        // ResponseDeserializer & RequestBodySerializer
        services.TryAddTransient<RequestQueryParamSerializer>(
            _ => new RestEaseRequestQueryParamSerializer());
        services.TryAddTransient<RequestBodySerializer>(
            _ => new RestEaseRequestBodySerializer());
        services.TryAddTransient<ResponseDeserializer>(
            _ => new RestEaseResponseDeserializer());

        configure?.Invoke(this);
    }

    // ConfigureXxx

    public RestEaseBuilder ConfigureHttpClient(
        Action<IServiceProvider, string?, HttpClientFactoryOptions> httpClientFactoryOptionsBuilder)
    {
        Services.Configure(httpClientFactoryOptionsBuilder);
        return this;
    }

    public static RestClient CreateRestClient(IServiceProvider c, HttpClient httpClient)
        => new(httpClient) {
            FormatProvider = CultureInfo.InvariantCulture,
            RequestBodySerializer = c.GetRequiredService<RequestBodySerializer>(),
            ResponseDeserializer = c.GetRequiredService<ResponseDeserializer>(),
            RequestQueryParamSerializer = c.GetRequiredService<RequestQueryParamSerializer>(),
        };

    // User-defined client-side services

    public RestEaseBuilder AddClient<TClient>(
        string? clientName = null, ServiceLifetime lifetime = ServiceLifetime.Transient)
        => AddClient(typeof(TClient), clientName, lifetime);

    public RestEaseBuilder AddClient(
        Type clientType, string? clientName = null, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (!(clientType is { IsInterface: true, IsVisible: true }))
            throw Errors.InterfaceTypeExpected(clientType, true, nameof(clientType));
        clientName ??= clientType.FullName ?? "";

        object Factory(IServiceProvider c) {
            var httpClientFactory = c.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            var client = CreateRestClient(c, httpClient).For(clientType);
            return client;
        }

        var descriptor = new ServiceDescriptor(clientType, Factory, lifetime);
        Services.TryAdd(descriptor);
        return this;
    }
}
