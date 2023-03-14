namespace NodeCommon.ApiModel;

public record TMTaskStateInfo(string Id, double Progress) : ITaskStateInfo;
public record TMTasksStateInfo(
    ImmutableArray<TMTaskStateInfo> Input, ImmutableArray<TMTaskStateInfo> Active, ImmutableArray<TMTaskStateInfo> Output, ImmutableArray<TMTaskStateInfo> Validation,
    int QueueSize, double AvgWaitTime, string ScGuid
);