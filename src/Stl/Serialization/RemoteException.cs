namespace Stl.Serialization;

#pragma warning disable SYSLIB0051

[Serializable]
public class RemoteException : Exception, ITransientException
{
    public ExceptionInfo ExceptionInfo { get; }

    public RemoteException()
        : this(ExceptionInfo.None)  { }
    public RemoteException(string message)
        : this(ExceptionInfo.None, message) { }
    public RemoteException(string message, Exception innerException)
        : this(ExceptionInfo.None, message, innerException) { }

    public RemoteException(ExceptionInfo exceptionInfo)
        => ExceptionInfo = exceptionInfo;
    public RemoteException(ExceptionInfo exceptionInfo, string message) : base(message)
        => ExceptionInfo = exceptionInfo;
    public RemoteException(ExceptionInfo exceptionInfo, string message, Exception innerException)
        : base(message, innerException)
        => ExceptionInfo = exceptionInfo;

    protected RemoteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
