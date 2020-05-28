using System;

namespace Stl.Fusion.Server.Internal
{
    public static class Errors
    {
        public static Exception AlreadyShared()
            => new InvalidOperationException(
                "Share method can be used just once per HTTP request."); 
        
    }
}
