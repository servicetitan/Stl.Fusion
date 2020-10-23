using System;

namespace Stl.Channels
{
    public enum ChannelCompletionMode
    {
        KeepOpen = 0,
        Complete = 1,
        CompleteAndPropagateError = 3,
    }
}
