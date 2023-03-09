namespace Telegram.MPlus;

public class MPlusClient
{
    internal readonly MPlusTaskManagerClient TaskManager;

    public MPlusClient(MPlusTaskManagerClient taskManager)
    {
        TaskManager = taskManager;
    }

    internal async Task<Uri> RequestFileDownloadLinkUsingFor(MPlusMediaFile mPlusMediaFile, Extension extension, ITaskApi taskApi)
        => new Uri((await taskApi.GetMPlusItemDownloadLinkAsync(mPlusMediaFile.Iid, extension, mPlusMediaFile.SessionId)).ThrowIfError());
}
