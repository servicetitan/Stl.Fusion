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
        private readonly ImageCodecInfo _jpegEncoder;
        private readonly EncoderParameters _jpegEncoderParameters;
        private readonly Rectangle _displayDimensions;
        private volatile Task<Screenshot> _prevScreenshotTask;

        public ScreenshotService()
        {
            _jpegEncoder = ImageCodecInfo
                .GetImageDecoders()
                .Single(codec => codec.FormatID == ImageFormat.Jpeg.Guid);  
            _jpegEncoderParameters = new EncoderParameters(1) {
                Param = {[0] = new EncoderParameter(Encoder.Quality, 50L)}
            };
            _displayDimensions = DisplayInfo.PrimaryDisplayDimensions 
                ?? new Rectangle(0, 0, 1920, 1080);  
            _prevScreenshotTask = ScreenshotAsync(128);
        }

        [ComputedServiceMethod(AutoInvalidateTime = 0.02)]
        public virtual async Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default)
        {
            // The logic here is a bit complicated b/c we send the last screenshot
            // here rather than wait for the current one.
            var next = ScreenshotAsync(width);
            var prev = Interlocked.Exchange(ref _prevScreenshotTask, next);
            var result = await prev.ConfigureAwait(false);
            if (result.Width != width)
                // Width changed, let's wait for the current one
                result = await next.ConfigureAwait(false);
            return result;
        }

        private Task<Screenshot> ScreenshotAsync(int width)
            => Task.Run(() => {
                var (w, h) = (_displayDimensions.Width, _displayDimensions.Height);
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
                bOut.Save(stream, _jpegEncoder, _jpegEncoderParameters);
                var bytes = stream.ToArray();
                var base64Content = Convert.ToBase64String(bytes);
                return new Screenshot(ow, oh, base64Content);
            }, CancellationToken.None);
    }
}
