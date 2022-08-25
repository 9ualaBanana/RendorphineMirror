using Timer = System.Timers.Timer;

namespace Telegram.Services;

public class TimerPlus : Timer
{
    public TimeSpan ElapsedTime => DateTime.Now - _creationTime;
    readonly DateTime _creationTime = DateTime.Now;

    public TimerPlus(double interval) : base(interval)
    {
    }
}
