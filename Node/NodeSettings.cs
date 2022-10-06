using Newtonsoft.Json.Linq;
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
        if (true)
        {
            var watching = new DatabaseValueDictionary<string, JObject>(nameof(WatchingTasks), x => x["Id"]!.Value<string>()!);
            foreach (var (key, value) in watching)
            {
                if (!value.ToString().Contains("WatchingTaskSource", StringComparison.Ordinal)) continue;

                value["Source"]!["$type"] = $"Common.Tasks.Watching.{value["Source"]!["Type"]!.Value<string>()!}WatchingTaskInputInfo, Common";
                value["Output"]!["$type"] = value["Output"]!["$type"]!.Value<string>()!.Replace("Node", "Common");
                var ininfo = value.ToObject<WatchingTask>(LocalApi.JsonSerializerWithType);

                watching.Save(value);
            }
        }


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

            try { target.AddRange(list); }
            catch (Exception ex) { LogManager.GetCurrentClassLogger().Error(ex); }
            finally
            {
                list.Bindable.Clear();
                list.Save();
                list.Delete();
            }
        }
        #endregion


        NodeGlobalState.Instance.WatchingTasks.Bind(WatchingTasks.Bindable);
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