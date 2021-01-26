using System;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Operations
{
    [Serializable]
    public class DbOperationFailedException : DbUpdateException
    {
        public DbOperationFailedException() { }
        public DbOperationFailedException(string message) : base(message) { }
        public DbOperationFailedException(string message, Exception innerException) : base(message, innerException) { }
        public DbOperationFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
