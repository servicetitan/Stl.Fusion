using System;

namespace Stl.Tests.Fusion.UIModels
{
    public class ServerTimeModel1
    {
        public DateTime? Time { get; }

        public ServerTimeModel1() { }
        public ServerTimeModel1(DateTime time) => Time = time;
    }

    // We need its second version to run the test w/ IComputed too
    public class ServerTimeModel2 : ServerTimeModel1
    {
        public ServerTimeModel2() { }
        public ServerTimeModel2(DateTime time) : base(time) { }
    }
}
