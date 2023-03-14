namespace UpdateServer;

public class ResettableDelay
{
    readonly object Locker = new();
    CancellableThread? Thread;

    public void ExecuteAfter(TimeSpan delay, Action func)
    {
        lock (Locker)
        {
            if (Thread is not null) Thread.Cancelled = true;
            Thread = new CancellableThread(delay, func);
        }
    }


    class CancellableThread
    {
        readonly Thread Thread;
        public bool Cancelled = false;

        public CancellableThread(TimeSpan delay, Action func)
        {
            Thread = new Thread(() =>
            {
                Thread.Sleep(delay);
                if (Cancelled) return;

                func();
            });

            Thread.IsBackground = true;
            Thread.Start();
        }
    }
}
