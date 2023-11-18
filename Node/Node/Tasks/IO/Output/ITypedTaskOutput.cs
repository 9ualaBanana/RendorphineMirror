namespace Node.Tasks.IO.Output;

public interface ITypedTaskOutput
{
    static abstract TaskOutputType Type { get; }
}
