namespace Node.Profiling;

public record NodeLoad(HardwareLoad HardwareLoad, Dictionary<TaskState, NodeLoadTask> Tasks);
public record NodeLoadTask(string Id, TaskState State, TaskAction Type, TaskTimes Times);
