namespace Common.Tasks;

public readonly struct WatchingTaskFileAddedEventArgs
{
    public readonly string FileName;
    public readonly ITaskInputInfo InputData;

    public WatchingTaskFileAddedEventArgs(string fileName, ITaskInputInfo inputData)
    {
        FileName = Path.GetFileName(fileName);
        InputData = inputData;
    }
}
