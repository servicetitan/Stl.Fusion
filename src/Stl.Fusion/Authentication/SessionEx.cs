using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public static class SessionEx
    {
        public static Session AssertNotNull(this Session? session)
            => session ?? throw Errors.NoSessionProvided();

        public static Session OrDefault(this Session? session, ISessionResolver sessionResolver)
        {
            if (session != null)
                return session;
            return sessionResolver.Session;
        }

        public static Session OrDefault(this Session? session, IServiceProvider services)
        {
            if (session != null)
                return session;
            var sessionResolver = services.GetRequiredService<ISessionResolver>();
            return sessionResolver.Session;
        }
    }
}
