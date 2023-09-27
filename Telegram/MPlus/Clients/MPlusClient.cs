namespace Telegram.MPlus.Clients;

public class MPlusClient
{
    public readonly MPlusTaskManagerClient TaskManager;
    public readonly MPlusTaskLauncherClient TaskLauncher;

    public MPlusClient(MPlusTaskManagerClient taskManager, MPlusTaskLauncherClient taskLauncher)
    {
        TaskManager = taskManager;
        TaskLauncher = taskLauncher;
    }
}
