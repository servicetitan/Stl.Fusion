namespace Stl.Interception;

public abstract class ArgumentListReader
{
    public abstract void OnStruct<T>(T item, int index);
    public abstract void OnObject(Type type, object? item, int index);
}
