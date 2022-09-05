namespace Common.Tasks.Model;

// TODO: ?
public class UserTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.User;
}
public class UserTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.User;
}
