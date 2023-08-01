namespace Node.Tasks;

// TEMPORARY; WILL BE REFACTORED
public interface IMPlusApi
{
    string TaskId { get; }
    string SessionId { get; }
    Api Api { get; }
}
public record MPlusApiService(string TaskId, string SessionId, Api Api) : IMPlusApi;


[Obsolete("DELETE")]
public interface ITaskExecutionContext : ILoggable
{
    IReadOnlyCollection<Plugin> Plugins { get; }

    /// <summary> Set task progress </summary>
    /// <param name="progress"> Progress value, 0-1 </param>
    void SetProgress(double progress);

    IMPlusApi? MPlusApi { get; }
}[Obsolete("DELETE")]
public static class TaskExecutionContextExtensions
{
    public static Plugin GetPlugin(this ITaskExecutionContext context, PluginType type) =>
        context.TryGetPlugin(type).ThrowIfNull();
    public static Plugin? TryGetPlugin(this ITaskExecutionContext context, PluginType type) =>
        context.Plugins.Where(p => p.Type == type).MaxBy(p => p.Version);
}