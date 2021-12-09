namespace Stl.Redis.Internal;

public static class Errors
{
    public static Exception SourceStreamError()
        => new InvalidOperationException("Source stream completed with an error.");
}
