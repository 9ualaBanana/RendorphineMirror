namespace Common.Tasks.Tasks.DTO;

public record TaskData<T>(
    string Type,
    IPluginActionData<T> MediaEditInfo) where T : IPluginActionData<T>
{
}
