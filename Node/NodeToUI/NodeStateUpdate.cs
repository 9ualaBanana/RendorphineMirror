namespace NodeToUI;

public class NodeStateUpdate
{
    public UpdateType Type { get; }
    public JToken Value { get; }

    public NodeStateUpdate(UpdateType type, JToken value)
    {
        Type = type;
        Value = value;
    }


    public enum UpdateType
    {
        State,
    }
}
