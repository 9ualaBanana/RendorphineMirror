namespace Node.Tasks.Exec;

public interface ITaskExecutor
{
    IEnumerable<IPluginAction> GetTasks();
}
