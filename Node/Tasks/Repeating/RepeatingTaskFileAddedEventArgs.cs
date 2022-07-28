namespace Node.Tasks.Repeating;

public readonly struct RepeatingTaskFileAddedEventArgs
{
    public readonly string FileName;
    public readonly ITaskInputInfo InputData;

    public RepeatingTaskFileAddedEventArgs(string fileName, ITaskInputInfo inputData)
    {
        FileName = Path.GetFileName(fileName);
        InputData = inputData;
    }
}
