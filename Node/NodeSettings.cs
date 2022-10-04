using Newtonsoft.Json;
using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueDictionary<string, ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueDictionary<string, ReceivedTask> CanceledTasks;
    public static readonly DatabaseValueDictionary<string, ReceivedTask> FailedTasks;
    public static readonly DatabaseValueDictionary<string, WatchingTask> WatchingTasks;
    public static readonly DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueDictionary<string, CompletedTask> CompletedTasks;

    static NodeSettings()
    {
        QueuedTasks = new(nameof(QueuedTasks), t => t.Id);
        CanceledTasks = new(nameof(CanceledTasks), t => t.Id);
        FailedTasks = new(nameof(FailedTasks), t => t.Id);
        WatchingTasks = new(nameof(WatchingTasks), t => t.Id);
        PlacedTasks = new(nameof(PlacedTasks), t => t.Id);
        CompletedTasks = new(nameof(CompletedTasks), t => t.TaskInfo.Id);

        #region migration
        load(nameof(QueuedTasks), QueuedTasks);
        load(nameof(CanceledTasks), CanceledTasks);
        load(nameof(FailedTasks), FailedTasks);
        load(nameof(WatchingTasks), WatchingTasks);
        load(nameof(PlacedTasks), PlacedTasks);
        load("PlacedTasks2", PlacedTasks);
        void load<T>(string source, DatabaseValueDictionary<string, T> target)
        {
            var list = new DatabaseValueList<T>(source);
            target.AddRange(list);
            list.Bindable.Clear();
            list.Delete();
        }
        #endregion


        WatchingTasks.Bindable.SubscribeChanged(() =>
            NodeGlobalState.Instance.WatchingTasks.SetRange(WatchingTasks.Select(x => JsonConvert.DeserializeObject<WatchingTaskInfo>(JsonConvert.SerializeObject(x))!))
        , true);

        NodeGlobalState.Instance.PlacedTasks.Bind(PlacedTasks.Bindable);
        NodeGlobalState.Instance.QueuedTasks.Bind(QueuedTasks.Bindable);
    }


    // TODO: remove after everyone migrates
    class DatabaseValueList<T> : DatabaseValueBase<IReadOnlyList<T>, BindableList<T>>, IEnumerable<T>
    {
        public int Count => Bindable.Count;

        public DatabaseValueList(string name, IEnumerable<T>? values = null) : base(name, new(values)) { }

        public IEnumerator<T> GetEnumerator() => Bindable.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}