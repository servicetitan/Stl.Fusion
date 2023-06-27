namespace Stl.Interception;

public abstract class ArgumentListWriter
{
    public abstract T OnStruct<T>(int index);
    public abstract object? OnObject(Type type, int index);
}
