namespace Node.Tasks.Exec.Output;

public interface IConvertibleToInput
{
    object ConvertToInput(int index, TaskAction action);
}
