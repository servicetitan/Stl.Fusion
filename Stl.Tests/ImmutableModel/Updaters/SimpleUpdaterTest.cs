using Stl.ImmutableModel;
using Stl.ImmutableModel.Updaters;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Updaters
{
    public class SimpleUpdaterTest : UpdaterTestBase
    {
        public SimpleUpdaterTest(ITestOutputHelper @out) : base(@out) { }

        protected override IUpdater<Index<ModelRoot>, ModelRoot> CreateUpdater(Index<ModelRoot> index) 
            => SimpleUpdater.New(index, index.Model);
    }
}