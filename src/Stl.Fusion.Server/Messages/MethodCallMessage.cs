using System;
using Stl.Reflection;

namespace Stl.Fusion.Server.Messages
{
    [Serializable]
    public class MethodCallMessage : GatewayMessage
    {
        public TypeRef ServiceType { get; set; }
        public string MethodName { get; set; } = "";
        public object[] Arguments { get; set; } = Array.Empty<object>();
    }
}
