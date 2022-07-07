namespace Common.Tasks.Tasks.DTO;

public abstract record MPlusTaskInfo : TaskInfo
{
    public override string Type => TaskType.MPlus.ToString();
}
