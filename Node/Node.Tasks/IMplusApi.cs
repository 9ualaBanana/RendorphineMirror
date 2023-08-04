namespace Node.Tasks;

public interface IMPlusApi
{
    string TaskId { get; }
    string SessionId { get; }
    Api Api { get; }
}