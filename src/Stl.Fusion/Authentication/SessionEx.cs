using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public static class SessionEx
    {
        public static Session AssertNotNull(this Session? session)
            => session ?? throw Errors.NoSessionProvided();
    }
}
