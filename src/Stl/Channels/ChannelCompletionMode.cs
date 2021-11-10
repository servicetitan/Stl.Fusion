namespace Stl.Channels;

[Flags]
public enum ChannelCompletionMode
{
    PropagateCompletion = 1,
    PropagateError = 2,
    PropagateCancellation = 4,
    Full = PropagateCompletion + PropagateError + PropagateCancellation,
}
