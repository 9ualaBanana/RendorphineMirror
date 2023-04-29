namespace NodeToUI;

public class TaskActionDescriber
{
    public readonly ImmutableArray<PluginType> RequiredPlugins;
    public readonly string Name;
    public readonly ObjectDescriber DataDescriber;

    public TaskActionDescriber(ImmutableArray<PluginType> requiredPlugins, string name, ObjectDescriber dataDescriber)
    {
        RequiredPlugins = requiredPlugins;
        Name = name;
        DataDescriber = dataDescriber;
    }
}
