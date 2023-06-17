using MemoryPack;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable]
public partial record TestCommand<TValue>(
    [property: DataMember] string Id,
    [property: DataMember] TValue? Value = null
) : ICommand<Unit> where TValue : class, IHasId<string>;
