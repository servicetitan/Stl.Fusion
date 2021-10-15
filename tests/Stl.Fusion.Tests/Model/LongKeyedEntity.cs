using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stl.Fusion.Tests.Model;

public record LongKeyedEntity : IHasId<long>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; init; }
}
