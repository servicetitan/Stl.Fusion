using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl.Samples.Blazor.Common.Services
{
    public class Screenshot
    {
        public int Width { get; }
        public int Height { get; }
        public string Base64Content { get; }

        [JsonConstructor]
        public Screenshot(int width, int height, string base64Content)
        {
            Width = width;
            Height = height;
            Base64Content = base64Content;
        }
    }

    public interface IScreenshotService
    {
        Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default);
    }
}
