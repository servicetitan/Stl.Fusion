namespace Stl
{
    /// <summary>
    /// An interface that indicates its implementor can be
    /// converted to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to which the current one can be converted.</typeparam>
    public interface IConvertibleTo<out T>
    {
        /// <summary>
        /// Converts the implementor of this interface to type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>A newly created instance of type <typeparamref name="T"/>.</returns>
        T Convert();
    }
}
