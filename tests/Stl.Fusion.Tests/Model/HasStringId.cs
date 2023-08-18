namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record HasStringId(
    [property: DataMember, MemoryPackOrder(0)] string Id
) : IHasId<string>;
