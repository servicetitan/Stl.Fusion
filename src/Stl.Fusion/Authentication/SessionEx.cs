using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public static class SessionEx
    {
        public static Disposable<Session?> Activate(this Session? session)
        {
            var currentLocal = Session.CurrentLocal;
            var oldValue = currentLocal.Value;
            currentLocal.Value = session;
            return Disposable.New(oldValue, oldValue1 => Session.CurrentLocal.Value = oldValue1);
        }

        public static Session AssertNotNull(this Session? session)
            => session ?? throw Errors.NoSessionProvided();
    }
}
