namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable]
public partial record HasStringId(
    [property: DataMember] string Id
) : IHasId<string>;
