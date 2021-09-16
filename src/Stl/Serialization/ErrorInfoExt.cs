using System;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Serialization
{
    public static class ErrorInfoExt
    {
#if !NETSTANDARD2_0
        [return: NotNullIfNotNull("error")]
#endif
        public static ErrorInfo? ToErrorInfo(this Exception? error)
            => error == null ? null : new ErrorInfo(error);
    }
}
