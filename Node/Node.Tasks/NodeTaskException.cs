namespace Node.Tasks;

[Obsolete("Use TaskFailedException instead")]
public class NodeTaskFailedException : TaskFailedException
{
    public NodeTaskFailedException(string message) : base(message) { }
}