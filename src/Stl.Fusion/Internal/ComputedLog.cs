using System;

namespace Stl.Fusion.Internal
{
    public static class ComputedLog
    {
        public static Action<string>? LogAction { get; set; }

        internal static void Log(string message)
        {
            if (LogAction != null)
                LogAction(message);
        }
    }
}