namespace Common.Tasks;

public class TaskActionDescriber
{
    public readonly PluginType Type;
    public readonly string Name;
    public readonly ObjectDescriber DataDescriber;

    public TaskActionDescriber(PluginType type, string name, ObjectDescriber dataDescriber)
    {
        Type = type;
        Name = name;
        DataDescriber = dataDescriber;
    }
}
