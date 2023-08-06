namespace Node.Tasks.IO;

public abstract class FileTaskOutputHandlerInfo<TData> : TaskOutputHandlerInfo<TData, ReadOnlyTaskFileList> where TData : ITaskOutputInfo
{

}
