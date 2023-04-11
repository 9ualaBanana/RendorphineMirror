namespace Node;

public static class AutoCleanup
{
    readonly static Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Start(bool cleanAllCompleted = false)
    {
        var now = DateTimeOffset.Now;
        Logger.Info($"[Cleanup] Started ({(cleanAllCompleted ? "complete" : "partial")})");

        Logger.Info($"[Cleanup] Checking completed tasks in db");
        foreach (var completed in NodeSettings.CompletedTasks.ToArray())
        {
            if (!cleanAllCompleted)
            {
                var days = (now - completed.Value.FinishTime).TotalDays;
                if (days < NodeSettings.TaskAutoDeletionDelayDays.Value)
                    continue;
            }

            Logger.Info($"[Cleanup] Removing expired completed task {completed.Key}");
            NodeSettings.CompletedTasks.Remove(completed.Key);
            OperationResult.WrapException(() => File.Delete(completed.Value.TaskInfo.FSDataDirectory())).LogIfError();
        }

        Logger.Info($"[Cleanup] Checking unknown qtasks in {ReceivedTask.FSTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.QueuedTasks.ContainsKey(taskid)) continue;
            if (NodeSettings.CompletedTasks.ContainsKey(taskid)) continue;

            Logger.Info($"[Cleanup] Deleting unknown qtask dir {dir}");
            Directory.Delete(dir, true);
        }

        Logger.Info($"[Cleanup] Checking unknown ptasks in {ReceivedTask.FSPlacedTaskDataDirectory()}");
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSPlacedTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.PlacedTasks.ContainsKey(taskid) || NodeSettings.CompletedTasks.ContainsKey(taskid)) continue;

            Logger.Info($"[Cleanup] Deleting unknown ptask dir {dir}");
            Directory.Delete(dir, true);
        }


        Logger.Info($"[Cleanup] Optimizing database");
        OperationResult.WrapException(() => Database.Instance.ExecuteNonQuery("PRAGMA optimize;")).LogIfError();
        OperationResult.WrapException(() => Database.Instance.ExecuteNonQuery("vacuum;")).LogIfError();

        Logger.Info($"[Cleanup] Completed");
    }
}
