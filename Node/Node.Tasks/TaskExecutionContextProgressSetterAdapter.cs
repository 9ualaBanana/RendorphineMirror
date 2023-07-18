namespace Node.Tasks;

public class TaskExecutionContextProgressSetterAdapter : IProgressSetter
{
    readonly ITaskExecutionContext Context;

    public TaskExecutionContextProgressSetterAdapter(ITaskExecutionContext context) => Context = context;

    public void Set(double progress) => Context.SetProgress(progress);
}
