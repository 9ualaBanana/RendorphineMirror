namespace Common.Tasks.Tasks;

public interface IPluginActionData { }
public interface IPluginActionData<TSelf> : IPluginActionData where TSelf : IPluginActionData<TSelf>
{
    static abstract ValueTask<TSelf> CreateDefault(CreateTaskData data);
}