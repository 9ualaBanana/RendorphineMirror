namespace NodeCommon.Tasks.Model;

public class MPlusItemTaskInputInfo : IMPlusTaskInputInfo
{
    public TaskInputType Type => TaskInputType.MPlusItem;
    public string Iid { get; }

    public MPlusItemTaskInputInfo(string id) => Iid = id;
}
