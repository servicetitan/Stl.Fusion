using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Stl.Fusion.EntityFramework.Conversion;

public sealed class UlidValueGenerator : ValueGenerator<Ulid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Ulid Next(EntityEntry entry) => Ulid.NewUlid();
}
