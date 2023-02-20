namespace NodeCommon.ApiModel;

public record ServerTaskState(TaskState State, double Progress, ITaskOutputInfo Output, TaskTimes Times, TaskServer? Server = null) : ITaskStateInfo;