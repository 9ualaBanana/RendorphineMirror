namespace NodeToUI;

public class TaskInputOutputDescriber
{
    public readonly string Type;
    public readonly ObjectDescriber Object;

    public TaskInputOutputDescriber(string type, ObjectDescriber @object)
    {
        Type = type;
        Object = @object;
    }
}
