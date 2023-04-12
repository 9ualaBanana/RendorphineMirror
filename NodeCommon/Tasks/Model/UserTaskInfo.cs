namespace NodeCommon.Tasks.Model;

// TODO: here just to not crash every time loading task list
public class UserTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.User;
}
public class UserTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.User;
}
