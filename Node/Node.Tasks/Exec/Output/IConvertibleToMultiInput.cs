namespace Node.Tasks.Exec.Output;

public interface IConvertibleToMultiInput
{
    IReadOnlyList<object> ConvertToInput(int index, TaskAction action);
}
