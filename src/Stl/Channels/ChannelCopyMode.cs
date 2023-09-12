namespace Stl.Channels;

[Flags]
public enum ChannelCopyMode
{
    CopyCompletion = 1,
    CopyError = 2,
    CopyCancellation = 4,
    CopyAll = CopyCompletion + CopyError + CopyCancellation,
    Silently = 64,
    CopyAllSilently = CopyAll + Silently,
}
