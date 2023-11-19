using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stl.Fusion.EntityFramework.Internal;

public class DbHintFormatterOptionsExtension(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dbHintFormatterType)
    : IDbContextOptionsExtension
{
    public Type DbHintFormatterType { get; } = dbHintFormatterType;

    public DbContextOptionsExtensionInfo Info
        => new DbHintFormatterExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
#pragma warning disable IL2072
        => services.AddSingleton(typeof(IDbHintFormatter), DbHintFormatterType);
#pragma warning restore IL2072

    public void Validate(IDbContextOptions options)
    { }
}
