namespace Node.Tasks;

public interface ITaskExecutionContext : ILoggable
{
    IReadOnlyCollection<Plugin> Plugins { get; }

    /// <summary> Set task progress </summary>
    /// <param name="progress"> Progress value, 0-1 </param>
    void SetProgress(double progress);
}
public static class TaskExecutionContextExtensions
{
    public static Plugin GetPlugin(this ITaskExecutionContext context, PluginType type) =>
        context.Plugins.First(p => p.Type == type);
    public static Plugin? TryGetPlugin(this ITaskExecutionContext context, PluginType type) =>
        context.Plugins.FirstOrDefault(p => p.Type == type);
}