using System;

namespace Stl
{
    public static class Skips
    {
        public static void ThrowNotImplementedException()
        {
            //throw new NotImplementedException();
        }
        
        public static void MissingFeature_NonCritical(string message = "")
        {
            //throw new NotImplementedException(message);
        }
    }
}