namespace Node;

public static class AutoCleanup
{
    readonly static Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Start()
    {
        var now = DateTimeOffset.Now;
        Logger.Info($"[Cleanup] Started");

        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.QueuedTasks.ContainsKey(taskid)) return;

            Logger.Info($"[Cleanup] Deleting unknown queued task dir {dir}");
            Directory.Delete(dir, true);
        }
        foreach (var dir in Directory.GetDirectories(ReceivedTask.FSPlacedTaskDataDirectory()))
        {
            var taskid = Path.GetFileName(dir);
            if (NodeSettings.PlacedTasks.ContainsKey(taskid) || NodeSettings.CompletedTasks.ContainsKey(taskid)) return;

            Logger.Info($"[Cleanup] Deleting unknown placed task dir {dir}");
            Directory.Delete(dir, true);
        }

        foreach (var completed in NodeSettings.CompletedTasks.ToArray())
        {
            var days = (completed.Value.FinishTime - now).TotalDays;
            if (days < NodeSettings.TaskAutoDeletionDelayDays.Value) continue;

            Logger.Info($"[Cleanup] Removing expired completed task {completed.Key}");
            NodeSettings.CompletedTasks.Remove(completed.Key);
            OperationResult.WrapException(() => File.Delete(completed.Value.TaskInfo.FSDataDirectory())).LogIfError();
        }



        Logger.Info($"[Cleanup] Finished");
    }
}
