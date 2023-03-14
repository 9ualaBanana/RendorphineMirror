namespace NodeCommon.ApiModel;

public record TMOldTaskStateInfo(string Id, TaskState State, ITaskOutputInfo? Output, string? ErrMsg) : ITaskStateInfo;