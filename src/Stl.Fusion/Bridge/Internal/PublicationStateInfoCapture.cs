namespace Stl.Fusion.Bridge.Internal;

public class PublicationStateInfoCapture : IDisposable
{
    private static readonly AsyncLocal<PublicationStateInfoCapture?> CurrentLocal = new();
    private readonly PublicationStateInfoCapture? _oldCurrent;

    public static PublicationStateInfoCapture? Current => CurrentLocal.Value;
    public PublicationStateInfo? Captured { get; private set; }

    public PublicationStateInfoCapture()
    {
        _oldCurrent = CurrentLocal.Value;
        CurrentLocal.Value = this;
    }

    public void Dispose() => CurrentLocal.Value = _oldCurrent;

    public void Capture(PublicationStateInfo publicationStateInfo)
        => Captured = publicationStateInfo;
}
