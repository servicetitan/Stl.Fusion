namespace Stl.Fusion.EntityFramework.Internal;

public static class Errors
{
    public static Exception DbContextIsReadOnly()
        => new InvalidOperationException("This DbContext is read-only.");

    public static Exception NoOperationsFrameworkServices()
        => new InvalidOperationException(
            "Operations Framework services aren't registered. " +
            "Call DbContextBuilder<TDbContext>.AddDbOperations before calling this method to add them.");

    public static Exception EntityNotFound<TEntity>()
        => EntityNotFound(typeof(TEntity));
    public static Exception EntityNotFound(Type entityType)
        => new KeyNotFoundException($"Requested {entityType.Name} entity is not found.");
}
