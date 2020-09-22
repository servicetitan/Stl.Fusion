using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public static class AuthContextEx
    {
        public static Disposable<AuthContext?> Activate(this AuthContext? authContext)
        {
            var currentLocal = AuthContext.CurrentLocal;
            var oldValue = currentLocal.Value;
            currentLocal.Value = authContext;
            return Disposable.New(oldValue, oldValue1 => AuthContext.CurrentLocal.Value = oldValue1);
        }

        public static AuthContext AssertNotNull(this AuthContext? authContext)
            => authContext ?? throw Errors.NoAuthContext();
    }
}
