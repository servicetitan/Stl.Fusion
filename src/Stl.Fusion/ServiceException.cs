namespace Stl.Fusion;

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
        OriginalExceptionType = (Type) info.GetValue(nameof(OriginalExceptionType), typeof(Type))!;
        var fStackTraceString = typeof(Exception).GetField("_stackTraceString",
            BindingFlags.Instance | BindingFlags.NonPublic);
        fStackTraceString!.SetValue(this, "");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        // Removing stack trace
        var tSerializationInfo = typeof(SerializationInfo);
        var mFindElement = tSerializationInfo.GetMethod("FindElement",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var stackTraceIndexObj = mFindElement?.Invoke(info, new Object[] {"StackTraceString"});
        if (stackTraceIndexObj is int stackTraceIndex) {
            var fValues = tSerializationInfo.GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
            var valuesObj = fValues?.GetValue(info);
            if (valuesObj is object?[] values)
                values[stackTraceIndex] = "";
        }
        info.AddValue(nameof(OriginalExceptionType), OriginalExceptionType, typeof(Type));
    }
}
