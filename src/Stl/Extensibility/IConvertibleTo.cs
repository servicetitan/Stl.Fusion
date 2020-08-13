namespace Stl.Extensibility
{
    public interface IConvertibleTo<out T>
    {
        T Convert();
    }
}
