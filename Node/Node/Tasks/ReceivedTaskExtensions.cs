namespace Node.Tasks;

public static class ReceivedTaskExtensions
{
    public static bool IsFromSameNode<TSettings>(this IRegisteredTask task, TSettings settings) where TSettings : IQueuedTasksStorage, IPlacedTasksStorage =>
        settings.QueuedTasks.ContainsKey(task.Id) && settings.PlacedTasks.ContainsKey(task.Id);

    public static void Populate(this DbTaskFullState task, ITaskStateInfo info)
    {
        if (info is TMTaskStateInfo tsi) task.Populate(tsi);
        if (info is TMOldTaskStateInfo osi) task.Populate(osi);
        if (info is ServerTaskState sts) task.Populate(sts);
    }
    public static void Populate(this DbTaskFullState task, TMTaskStateInfo info) => task.Progress = info.Progress;
    public static void Populate(this DbTaskFullState task, TMOldTaskStateInfo info)
    {
        task.State = info.State;
        if (info.Output is not null)
            JsonSettings.Default.Populate(JObject.FromObject(info.Output).CreateReader(), task.Output);
    }
    public static void Populate(this DbTaskFullState task, ServerTaskState info)
    {
        task.State = info.State;
        task.Progress = info.Progress;
        task.Times = info.Times;
        // task.Server = info.Server;

        if (info.Output is not null)
            JsonSettings.Default.Populate(JObject.FromObject(info.Output).CreateReader(), task.Output);
    }
}
