namespace Node;

public static class AutoCleanup
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void CleanForLowFreeSpace()
    {
        Info($"[Cleanup] Low free space cleanup started");

        CleanQueuedTasks();
        CleanPlacedTasks();
    }
    public static void Start()
    {
        var now = DateTimeOffset.Now;
        Info($"[Cleanup] Started");

        CleanCompletedTasks();
        CleanQueuedTasks();
        CleanPlacedTasks();

        Info($"[Cleanup] Optimizing database");
        OperationResult.WrapException(() => Database.Instance.ExecuteNonQuery("PRAGMA optimize;")).LogIfError();
        OperationResult.WrapException(() => Database.Instance.ExecuteNonQuery("vacuum;")).LogIfError();

        Info($"[Cleanup] Completed");
    }

    static void CleanCompletedTasks()
    {
        Info($"[Cleanup] Cleaning completed tasks in db");
        var now = DateTimeOffset.Now;

        using var transaction = NodeSettings.CompletedTasks.Database.BeginTransaction();
        foreach (var completed in NodeSettings.CompletedTasks.ToArray())
        {
            var days = (now - completed.Value.FinishTime).TotalDays;
            if (days < NodeSettings.TaskAutoDeletionDelayDays.Value)
                continue;

            Info($"[Cleanup] Removing expired completed task {completed.Key}");
            NodeSettings.CompletedTasks.Remove(completed.Key);
            OperationResult.WrapException(() => File.Delete(completed.Value.TaskInfo.FSDataDirectory())).LogIfError();
        }

        transaction.Commit();
    }
    static void CleanQueuedTasks()
    {
        Info($"[Cleanup] Cleaning unknown qtasks in {ReceivedTask.FSTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.QueuedTasks.ContainsKey(taskid)) continue;
            if (NodeSettings.CompletedTasks.ContainsKey(taskid)) continue;

            new Thread(() => Info($"[Cleanup] Deleting unknown qtask dir {dir}")) { IsBackground = true }.Start();
            Directory.Delete(dir, true);
        }
    }
    static void CleanPlacedTasks()
    {
        Info($"[Cleanup] Cleaning unknown ptasks in {ReceivedTask.FSPlacedTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSPlacedTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.PlacedTasks.ContainsKey(taskid) || NodeSettings.CompletedTasks.ContainsKey(taskid)) continue;

            new Thread(() => Info($"[Cleanup] Deleting unknown ptask dir {dir}")) { IsBackground = true }.Start();
            Directory.Delete(dir, true);
        }
    }


    // creating new thread for logging in case of 0 bytes free space available, so the logger wouldn't be able to write into log file and might just freeze
    static void Info(string text) => new Thread(() => Logger.Info(text)) { IsBackground = true }.Start();
}
