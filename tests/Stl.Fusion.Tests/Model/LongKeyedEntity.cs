using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable]
public partial record LongKeyedEntity : IHasId<long>
{
    [DataMember, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; init; }
}
