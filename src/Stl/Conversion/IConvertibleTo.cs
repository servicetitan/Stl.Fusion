namespace Stl.Conversion;

/// <summary>
/// An interface that indicates its implementor can be
/// converted to type <typeparamref name="TTarget"/>.
/// </summary>
/// <typeparam name="TTarget">Type to which the current one can be converted.</typeparam>
public interface IConvertibleTo<out TTarget>
{
    /// <summary>
    /// Converts the implementor of this interface to type <typeparamref name="TTarget"/>.
    /// </summary>
    /// <returns>A newly created instance of type <typeparamref name="TTarget"/>.</returns>
    TTarget Convert();
}
