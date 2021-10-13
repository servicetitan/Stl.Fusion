using System;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Serialization
{
    public static class ExceptionInfoExt
    {
        public static ExceptionInfo ToExceptionInfo(this Exception? error)
            => error == null ? default : new ExceptionInfo(error);
    }
}
