namespace NodeCommon.ApiModel;

public record ServerTaskState(TaskState State, double Progress, TaskAction Type, ITaskOutputInfo Output, TaskTimes Times, TaskServer? Server = null) : ITaskStateInfo;