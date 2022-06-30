namespace Common.Tasks.Tasks;

public interface IPluginActionData { }
public interface IPluginActionData<TSelf> : IPluginActionData where TSelf : IPluginActionData<TSelf>
{
    static abstract TSelf CreateDefault(string[] files);
}