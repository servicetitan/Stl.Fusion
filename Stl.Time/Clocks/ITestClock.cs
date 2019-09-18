namespace Stl.Time.Clocks 
{
    public interface ITestClock : IClock
    {
        TestClockSettings Settings { get; set; }
    }
}
