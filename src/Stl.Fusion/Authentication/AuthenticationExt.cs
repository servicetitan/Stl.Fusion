using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public static class AuthenticationExt
    {
        public static User MustBeAuthenticated(this User? user)
        {
            if (!(user?.IsAuthenticated ?? false))
                throw Errors.NotAuthenticated();
            return user;
        }

        public static SessionInfo MustBeAuthenticated(this SessionInfo? sessionInfo)
        {
            if (!(sessionInfo?.IsAuthenticated ?? false))
                throw Errors.NotAuthenticated();
            return sessionInfo;
        }
    }
}
