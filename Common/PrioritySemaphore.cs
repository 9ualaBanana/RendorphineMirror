namespace Common;

public class PrioritySemaphore
{
    public int CurrentCount => Volatile.Read(ref _CurrentCount);

    readonly PriorityQueue<TaskCompletionSource, long> Queue = new();
    readonly int MaxCount;
    int _CurrentCount;

    public PrioritySemaphore(int count) : this(count, count) { }
    public PrioritySemaphore(int initialCount, int maxCount)
    {
        _CurrentCount = initialCount;
        MaxCount = maxCount;
    }

    public Task WaitAsync(long priority)
    {
        lock (Queue)
        {
            if (_CurrentCount > 0)
            {
                _CurrentCount--;
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Queue.Enqueue(tcs, priority);
            return tcs.Task;
        }
    }

    public void Release()
    {
        TaskCompletionSource tcs;

        lock (Queue)
        {
            if (Queue.Count == 0)
            {
                if (_CurrentCount >= MaxCount)
                    throw new SemaphoreFullException();
                _CurrentCount++;

                return;
            }
            tcs = Queue.Dequeue();
        }

        tcs.SetResult();
    }
}
