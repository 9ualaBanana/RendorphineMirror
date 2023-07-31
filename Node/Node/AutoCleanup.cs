namespace Node;

public class AutoCleanup
{
    readonly IQueuedTasksStorage QueuedTasks;
    readonly ICompletedTasksStorage CompletedTasks;
    readonly IPlacedTasksStorage PlacedTasks;
    readonly ILogger Logger;

    public AutoCleanup(IQueuedTasksStorage queuedTasks, ICompletedTasksStorage completedTasks, IPlacedTasksStorage placedTasks, ILogger<AutoCleanup> logger)
    {
        QueuedTasks = queuedTasks;
        CompletedTasks = completedTasks;
        PlacedTasks = placedTasks;
        Logger = logger;
    }

    public void Start()
    {
        new Thread(() =>
        {
            while (true)
            {
                OperationResult.WrapException(Execute).LogIfError(Logger);
                Thread.Sleep(TimeSpan.FromDays(1));
            }
        })
        { IsBackground = true }.Start();

        new Thread(() =>
        {
            while (true)
            {
                var root = Path.GetPathRoot(ReceivedTask.FSTaskDataDirectory());
                var drive = DriveInfo.GetDrives().First(d => d.RootDirectory.Name == root);

                if (drive.AvailableFreeSpace < 16L * 1024 * 1024 * 1024)
                {
                    Info($"Low free space ({drive.AvailableFreeSpace / 1024 / 1024f} MB), starting a cleanup..");
                    OperationResult.WrapException(CleanForLowFreeSpace).LogIfError(Logger);
                }

                Thread.Sleep(60 * 1000);
            }
        })
        { IsBackground = true }.Start();
    }

    public void CleanForLowFreeSpace()
    {
        Info($"[Cleanup] Low free space cleanup started");

        CleanQueuedTasks();
        CleanPlacedTasks();
    }
    public void Execute()
    {
        Info($"[Cleanup] Started");

        CleanCompletedTasks();
        CleanQueuedTasks();
        CleanPlacedTasks();

        Info($"[Cleanup] Completed");
    }

    void CleanCompletedTasks()
    {
        Info($"[Cleanup] Cleaning completed tasks in db");
        var now = DateTimeOffset.Now;

        using var transaction = CompletedTasks.CompletedTasks.Database.BeginTransaction();
        foreach (var completed in CompletedTasks.CompletedTasks.ToArray())
        {
            var days = (now - completed.Value.FinishTime).TotalDays;
            if (days < Settings.TaskAutoDeletionDelayDays.Value)
                continue;

            Info($"[Cleanup] Removing expired completed task {completed.Key}");
            CompletedTasks.CompletedTasks.Remove(completed.Key);
            OperationResult.WrapException(() => File.Delete(completed.Value.TaskInfo.FSDataDirectory())).LogIfError(Logger);
        }

        transaction.Commit();
    }
    void CleanQueuedTasks()
    {
        Info($"[Cleanup] Cleaning unknown qtasks in {ReceivedTask.FSTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (QueuedTasks.QueuedTasks.ContainsKey(taskid)) continue;
            if (CompletedTasks.CompletedTasks.ContainsKey(taskid)) continue;

            new Thread(() => Info($"[Cleanup] Deleting unknown qtask dir {dir}")) { IsBackground = true }.Start();
            Directory.Delete(dir, true);
        }
    }
    void CleanPlacedTasks()
    {
        Info($"[Cleanup] Cleaning unknown ptasks in {ReceivedTask.FSPlacedTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSPlacedTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (PlacedTasks.PlacedTasks.ContainsKey(taskid) || CompletedTasks.CompletedTasks.ContainsKey(taskid)) continue;

            new Thread(() => Info($"[Cleanup] Deleting unknown ptask dir {dir}")) { IsBackground = true }.Start();
            Directory.Delete(dir, true);
        }
    }


    // creating new thread for logging in case of 0 bytes free space available, so the logger wouldn't be able to write into log file and might just freeze
    void Info(string text) => new Thread(() => Logger.Info(text)) { IsBackground = true }.Start();
}
