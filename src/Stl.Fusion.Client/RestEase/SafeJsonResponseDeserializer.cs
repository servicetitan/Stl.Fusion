using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using RestEase;
using Stl.Serialization;

namespace Stl.Fusion.Client.RestEase
{
    public class SafeJsonResponseDeserializer : ResponseDeserializer
    {
        protected IServiceProvider Services { get; }

        public SafeJsonResponseDeserializer(IServiceProvider services) 
            => Services = services;

        public override T Deserialize<T>(
            string? content, HttpResponseMessage response, 
            ResponseDeserializerInfo info)
        {
            var serializer = Services.GetRequiredService<SafeJsonNetSerializer<T>>();
            return serializer.Deserialize(content ?? ""); 
        } 
    }
}
