namespace Stl
{
    public interface IConvertibleTo<out T>
    {
        T Convert();
    }
}
