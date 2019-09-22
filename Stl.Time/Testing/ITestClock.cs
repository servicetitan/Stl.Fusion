namespace Stl.Time.Testing 
{
    public interface ITestClock : IClock
    {
        TestClockSettings Settings { get; set; }
    }
}
