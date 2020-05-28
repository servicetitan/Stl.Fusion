using System.Net.Http;
using RestEase;

namespace Stl.Fusion.Client.RestEase.Internal
{
    internal readonly struct DeserializeMethodArgs
    {
        public readonly string? Content;
        public readonly HttpResponseMessage Response; 
        public readonly ResponseDeserializerInfo Info;

        public DeserializeMethodArgs(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
        {
            Content = content;
            Response = response;
            Info = info;
        }

        public void Deconstruct(out string? content, out HttpResponseMessage response, out ResponseDeserializerInfo info)
        {
            content = Content;
            response = Response;
            info = Info;
        }
    }
}
