using Node.P2P.Upload;

namespace Node.Tasks.Executor;

public class MPlusTaskResultUploader : ITaskResultUploader
{
    public Task Upload(ReceivedTask task, string file) => PacketsTransporter.UploadAsync(new MPlusUploadSessionData(file, task.Id));
}
