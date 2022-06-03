using Timer = System.Timers.Timer;

namespace ReepoBot.Services;

public class TimerPlus : Timer
{
    readonly DateTime _creationTime;
    public TimeSpan ElapsedTime => DateTime.Now - _creationTime;

    public TimerPlus(double interval) : base(interval)
    {
        _creationTime = DateTime.Now;
    }
}
