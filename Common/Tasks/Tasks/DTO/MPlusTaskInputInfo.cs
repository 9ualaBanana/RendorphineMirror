namespace Common.Tasks.Tasks.DTO;

public record MPlusTaskInputInfo : TaskInfo
{
    public string Iid { get; }

    public MPlusTaskInputInfo(string iid) : base(TaskType.MPlus)
    {
        Iid = iid;
    }
}
