using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Stl.Fusion
{
    [Serializable]
    public class ServiceException : ApplicationException
    {
        public Type? OriginalExceptionType { get; private set; }

        public ServiceException(string message) : base(message)
            => OriginalExceptionType = null;
        public ServiceException(Type? originalExceptionType, string message) : base(message)
            => OriginalExceptionType = originalExceptionType;
        public ServiceException(Exception original) : base(original.Message)
            => OriginalExceptionType = original?.GetType();

        protected ServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OriginalExceptionType = (Type) info.GetValue(nameof(OriginalExceptionType), typeof(Type));
            GetType().GetField("_stackTraceString").SetValue(this, "");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Removing stack trace
            var mFindElement = info.GetType().GetMethod("FindElement", BindingFlags.Instance | BindingFlags.NonPublic);
            var stackTraceIndexObj = mFindElement?.Invoke(info, new Object[] {"StackTraceString"});
            if (stackTraceIndexObj is int stackTraceIndex) {
                var fValues = info.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
                var valuesObj = fValues?.GetValue(info);
                if (valuesObj is object?[] values)
                    values[stackTraceIndex] = "";
            }
            info.AddValue(nameof(OriginalExceptionType), OriginalExceptionType, typeof(Type));
        }
    }
}
