namespace Node.Tasks.Exec;

public class ThrottledProgressSetter : ITaskProgressSetter
{
    readonly TimeSpan ProgressSendDelay;
    readonly ITaskProgressSetter Progress;

    public ThrottledProgressSetter(TimeSpan progressSendDelay, ITaskProgressSetter progress)
    {
        ProgressSendDelay = progressSendDelay;
        Progress = progress;
    }

    DateTime ProgressWriteTime = DateTime.MinValue;
    public void Set(double progress)
    {
        var now = DateTime.Now;
        if (progress >= .98 || ProgressWriteTime < now)
        {
            Progress.Set(progress);
            ProgressWriteTime = DateTime.Now + ProgressSendDelay;
        }
    }
}
