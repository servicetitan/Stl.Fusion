using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Stl.OS;

namespace Stl.Fusion.Tests.Services;

[DataContract]
public record Screenshot
{
    [DataMember] public int Width { get; init; }
    [DataMember] public int Height { get; init; }
    [DataMember] public Moment CapturedAt { get; init; }
    [DataMember] public Base64Encoded Image { get; init; } = "";
}

public interface IScreenshotService
{
    [ComputeMethod(MinCacheDuration = 0.3)]
    Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken = default);
}

[RegisterComputeService(typeof(IScreenshotService), Scope = ServiceScope.Services)]
public class ScreenshotService : IScreenshotService
{
#pragma warning disable CA1416
    private readonly ImageCodecInfo _jpegEncoder;
    private readonly EncoderParameters _jpegEncoderParameters;
    private readonly Rectangle _displayDimensions;

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
    }

    // [ComputeMethod(MinCacheDuration = 0.3)]
    public virtual async Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken = default)
    {
        var bScreen = await GetScreenshot(cancellationToken).ConfigureAwait(false);

        // The code below scales a full-resolution screenshot to a desirable resolution
        var (w, h) = (_displayDimensions.Width, _displayDimensions.Height);
        var ow = width;
        var oh = h * ow / w;
        using var bOut = new Bitmap(ow, oh);
        using var gOut = Graphics.FromImage(bOut);
        gOut.CompositingQuality = CompositingQuality.HighSpeed;
        gOut.InterpolationMode = InterpolationMode.Default;
        gOut.CompositingMode = CompositingMode.SourceCopy;
        gOut.DrawImage(bScreen, 0, 0, ow, oh);

        using var stream = new MemoryStream();
        bOut.Save(stream, _jpegEncoder, _jpegEncoderParameters);
        var bytes = stream.ToArray();
        var base64Content = Convert.ToBase64String(bytes);
        return new Screenshot {
            Width = ow,
            Height = oh,
            CapturedAt = SystemClock.Now,
            Image = base64Content
        };
    }

    [ComputeMethod(AutoInvalidationDelay = 0.01)]
    protected virtual Task<Bitmap> GetScreenshot(CancellationToken cancellationToken = default)
    {
        // This method takes a full-resolution screenshot
        var (w, h) = (_displayDimensions.Width, _displayDimensions.Height);
        var bScreen = new Bitmap(w, h);
        using var gScreen = Graphics.FromImage(bScreen);
        gScreen.CopyFromScreen(0, 0, 0, 0, bScreen.Size);
        Computed.GetCurrent()!.Invalidated += c => {
            Task.Delay(2000, default).ContinueWith(_ => {
                // Let's dispose these values in 2 seconds
                var computed = (Computed<Bitmap>) c;
                if (computed.HasValue)
                    computed.Value.Dispose();
            }, TaskScheduler.Default);
        };
        return Task.FromResult(bScreen);
    }
#pragma warning restore CA1416
}
