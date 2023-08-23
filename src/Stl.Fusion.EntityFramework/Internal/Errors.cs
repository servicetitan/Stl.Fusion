using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Internal;

public static class Errors
{
    public static Exception CreateCommandDbContextIsCalledFromInvalidationCode()
        => new InvalidOperationException(
            $"{nameof(DbHub<DbContext>.CreateCommandDbContext)} is called from the invalidation code. " +
            $"If you want to read the data there, use {nameof(DbHub<DbContext>.CreateDbContext)} instead.");
    public static Exception DbContextIsReadOnly()
        => new InvalidOperationException("This DbContext is read-only.");

    public static Exception NoOperationsFrameworkServices()
        => new InvalidOperationException(
            "Operations Framework services aren't registered. " +
            "Call DbContextBuilder<TDbContext>.AddDbOperations before calling this method to add them.");

    public static Exception TenantCannotBeChanged()
        => new InvalidOperationException("DbContext is already created, so its Tenant property cannot be changed at this point.");
    public static Exception NonDefaultTenantIsUsedInSingleTenantMode()
        => new NotSupportedException(
            "A tenant other than Tenant.Default is attempted to be used in single-tenant mode.");
    public static Exception DefaultTenantCanOnlyBeUsedInSingleTenantMode()
        => new NotSupportedException(
            "Tenant.Default can only be used in single-tenant mode (with SingleTenantResolver).");

    public static Exception EntityNotFound<TEntity>()
        => EntityNotFound(typeof(TEntity));
    public static Exception EntityNotFound(Type entityType)
        => new KeyNotFoundException($"Requested {entityType.GetName()} entity is not found.");

    public static Exception InvalidUserId()
        => new FormatException("Invalid UserId.");
    public static Exception UserIdRequired()
        => new FormatException("UserId is None, even though a valid one is expected here.");

    public static Exception UnsupportedDbHint(DbHint hint)
        => new NotSupportedException($"Unsupported DbHint: {hint}");
}
