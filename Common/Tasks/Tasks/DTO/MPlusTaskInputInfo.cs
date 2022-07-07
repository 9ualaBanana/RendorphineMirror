namespace Common.Tasks.Tasks.DTO;

public record MPlusTaskInputInfo : MPlusTaskInfo
{
    public string Iid { get; }

    public MPlusTaskInputInfo(string iid)
    {
        Iid = iid;
    }
}
