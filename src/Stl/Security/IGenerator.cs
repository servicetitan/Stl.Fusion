namespace Stl.Security
{
    public interface IGenerator<out T>
    {
        T Next();
    }
}
