using System;
using Stl.Reflection;

namespace Stl.Serialization
{
    public record ErrorInfo
    {
        private static readonly TypeRef ExceptionType = typeof(Exception);

        public TypeRef Type { get; init; } = ExceptionType;
        public string Message { get; init; } = "";

        public ErrorInfo() { }
        public ErrorInfo(Exception e)
        {
            Type = e.GetType();
            Message = e.Message;
        }

    }
}
