using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace Stl.Tests.Fusion.Services
{
    public interface IScreenshotService
    {
        Task<string> GetScreenshotAsync(int width, CancellationToken cancellationToken = default);
    }

    public class ScreenshotService : IScreenshotService, IComputedService
    {
        protected ImageCodecInfo JpegEncoder { get; }
        protected EncoderParameters JpegEncoderParameters { get; }

        public ScreenshotService()
        {
            JpegEncoder = ImageCodecInfo
                .GetImageDecoders()
                .Single(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            JpegEncoderParameters = new EncoderParameters(1) {
                Param = {[0] = new EncoderParameter(Encoder.Quality, 50L)}
            };
        }

        [ComputedServiceMethod(AutoInvalidateTime = 0.05)]
        public virtual async Task<string> GetScreenshotAsync(int width, CancellationToken cancellationToken = default)
        {
            using var screen = Graphics.FromHwnd(IntPtr.Zero);
            var vcb = screen.VisibleClipBounds;
            var (w, h) = ((int) vcb.Width, (int) vcb.Height);
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
            await using var stream = new MemoryStream();
            bOut.Save(stream, JpegEncoder, JpegEncoderParameters);
            var bytes = stream.ToArray();
            return Convert.ToBase64String(bytes);
        }
    }
}
