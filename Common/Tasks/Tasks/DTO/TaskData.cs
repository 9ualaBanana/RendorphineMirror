namespace Common.Tasks.Tasks.DTO;

public record TaskData<T>(
    string Type,
    T MediaEditInfo) where T : IPluginActionData
{
}
