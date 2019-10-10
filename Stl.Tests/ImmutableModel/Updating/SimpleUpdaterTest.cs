using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Updating
{
    public class SimpleUpdaterTest : UpdaterTestBase
    {
        public SimpleUpdaterTest(ITestOutputHelper @out) : base(@out) { }

        protected override IModelUpdater<ModelRoot> CreateModelUpdater(IIndex<ModelRoot> index) 
            => SimpleModelUpdater.New(index);
    }
}
