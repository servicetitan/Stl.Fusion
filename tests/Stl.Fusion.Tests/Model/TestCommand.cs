namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record TestCommand<TValue>(
    [property: DataMember, MemoryPackOrder(0)] string Id,
    [property: DataMember, MemoryPackOrder(1)] TValue? Value = null
) : ICommand<Unit> where TValue : class, IHasId<string>;
