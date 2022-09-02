namespace Node.Tasks.Models;

public class TorrentTaskInfo
{
    public static ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskInputInfo) task.Input;

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
            return info.Link.AsVTask();

        throw new NotImplementedException();
    }
    public static ValueTask UploadResult(ReceivedTask task, string file, string? postfix, CancellationToken cancellationToken)
    {
        var info = (TorrentTaskOutputInfo) task.Input;

        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
        {
            var filename = Path.GetFileNameWithoutExtension(info.FileName) + postfix + Path.GetExtension(info.FileName);

            Directory.CreateDirectory(info.Directory);
            File.Copy(file, Path.Combine(info.Directory, filename), true);
            return ValueTask.CompletedTask;
        }

        throw new NotImplementedException();
    }
}
