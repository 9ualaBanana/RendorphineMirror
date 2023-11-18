namespace Node.Tasks.IO.Input;

public interface ITypedTaskInput
{
    static abstract TaskInputType Type { get; }
}
