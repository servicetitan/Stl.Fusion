using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record LongKeyedEntity : IHasId<long>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [DataMember, MemoryPackOrder(0)]
    public long Id { get; init; }
}
