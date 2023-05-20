namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueDictionary<string, ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueDictionary<string, WatchingTask> WatchingTasks;
    public static readonly DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueDictionary<string, CompletedTask> CompletedTasks;

    static NodeSettings()
    {
        QueuedTasks = new(new Database(Directories.DataFile("queued.db")), nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(new Database(Directories.DataFile("watching.db")), nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(new Database(Directories.DataFile("placed.db")), nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(new Database(Directories.DataFile("completed.db")), nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);


        {
            var configdb = new Database(Directories.DataFile("config.db"));

            migrate(QueuedTasks, nameof(QueuedTasks), t => t.Id);
            migrate(WatchingTasks, nameof(WatchingTasks), t => t.Id);
            migrate(PlacedTasks, nameof(PlacedTasks), t => t.Id);
            migrate(CompletedTasks, nameof(CompletedTasks), t => t.TaskInfo.Id);


            void migrate<TValue>(DatabaseValueDictionary<string, TValue> newdict, string name, Func<TValue, string> keyFunc)
            {
                try
                {
                    var dict = new DatabaseValueDictionary<string, TValue>(configdb, name, keyFunc, serializer: JsonSettings.Default);
                    if (dict.Count == 0) return;

                    newdict.AddRange(dict.Values);
                    dict.Clear();
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
            }
        }
    }
}