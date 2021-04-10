namespace Stl
{
    /// <summary>
    /// An interface that indicates its implementor has an identifier of type <typeparamref name="TId"/>.
    /// </summary>
    /// <typeparam name="TId">Type of <see cref="Id"/> property value.</typeparam>
    public interface IHasId<out TId>
    {
        /// <summary>
        /// Instance's identifier.
        /// </summary>
        TId Id { get; }
    }
}
