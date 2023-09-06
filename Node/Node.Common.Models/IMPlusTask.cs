namespace Node.Common.Models;

public interface IMPlusTask : IRegisteredTaskApi
{
    double Progress { get; }
    TaskState State { get; }
}
