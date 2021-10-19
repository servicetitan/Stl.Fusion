namespace Stl.Time.Testing;

public interface ITestClock : IMomentClock
{
    TestClockSettings Settings { get; set; }
}
