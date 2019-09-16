using Stl.ImmutableModel;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Updating
{
    public class QueuingUpdaterTest : UpdaterTestBase
    {
        public QueuingUpdaterTest(ITestOutputHelper @out) : base(@out) { }

        protected override IUpdater<UpdatableIndex<ModelRoot>, ModelRoot> CreateUpdater(UpdatableIndex<ModelRoot> index) 
            => QueuingUpdater.New(index, index.Model);
    }
}
