using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.EntityFramework.Internal;

public class DbHintFormatterOptionsExtension : IDbContextOptionsExtension
{
    public Type DbHintFormatterType { get; }

    public DbContextOptionsExtensionInfo Info
        => new DbHintFormatterExtensionInfo(this);

    public DbHintFormatterOptionsExtension(Type dbHintFormatterType)
        => DbHintFormatterType = dbHintFormatterType;

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(typeof(IDbHintFormatter), DbHintFormatterType);
    }

    public void Validate(IDbContextOptions options)
    { }
}
