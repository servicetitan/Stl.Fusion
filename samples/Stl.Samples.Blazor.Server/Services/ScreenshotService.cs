using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.OS;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Services
{
    public class ScreenshotService : IScreenshotService, IComputedService
    {
        protected ImageCodecInfo JpegEncoder { get; }
        protected EncoderParameters JpegEncoderParameters { get; }
        protected Rectangle DisplayDimensions { get; }
        protected Task<Screenshot> PrevScreenshotTask;

        public ScreenshotService()
        {
            JpegEncoder = ImageCodecInfo
                .GetImageDecoders()
                .Single(codec => codec.FormatID == ImageFormat.Jpeg.Guid);  
            JpegEncoderParameters = new EncoderParameters(1) {
                Param = {[0] = new EncoderParameter(Encoder.Quality, 50L)}
            };
            DisplayDimensions = DisplayInfo.PrimaryDisplayDimensions 
                ?? new Rectangle(0, 0, 1920, 1080);  
            PrevScreenshotTask = ScreenshotAsync(128);
        }

        [ComputedServiceMethod]
        public virtual async Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default)
        {
            // The logic here is a bit complicated b/c we send the last screenshot
            // here rather than wait for the current one.
            var computed = Computed.GetCurrent();
            var next = ScreenshotAsync(width);
            var prev = Interlocked.Exchange(ref PrevScreenshotTask, next);
            var result = await prev.ConfigureAwait(false);
            if (result.Width != width)
                // Width changed, let's wait for the current one
                result = await next.ConfigureAwait(false);
            Task.Delay(100, CancellationToken.None)
                .ContinueWith(_ => computed!.Invalidate(), CancellationToken.None)
                .Ignore();
            return result;
        }

        private Task<Screenshot> ScreenshotAsync(int width)
            => Task.Run(() => {
                var (w, h) = (DisplayDimensions.Width, DisplayDimensions.Height);
                using var bScreen = new Bitmap(w, h);
                using var gScreen = Graphics.FromImage(bScreen);
                gScreen.CopyFromScreen(0, 0, 0, 0, bScreen.Size);
                var ow = width;
                var oh = h * ow / w;
                using var bOut = new Bitmap(ow, oh);
                using var gOut = Graphics.FromImage(bOut);
                gOut.CompositingQuality = CompositingQuality.HighSpeed;
                gOut.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gOut.CompositingMode = CompositingMode.SourceCopy;
                gOut.DrawImage(bScreen, 0, 0, ow, oh);
                using var stream = new MemoryStream();
                bOut.Save(stream, JpegEncoder, JpegEncoderParameters);
                var bytes = stream.ToArray();
                var base64Content = Convert.ToBase64String(bytes);
                return new Screenshot(ow, oh, base64Content);
            }, CancellationToken.None);
    }
}
