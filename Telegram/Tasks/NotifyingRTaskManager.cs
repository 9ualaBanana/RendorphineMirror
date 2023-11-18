using Telegram.Localization;
using Telegram.MPlus.Clients;
using Telegram.TrialUsers;

namespace Telegram.Tasks;

public partial class NotifyingRTaskManager : RTaskManager
{
    readonly Notifications.RTask _notifications;

    public NotifyingRTaskManager(
        Notifications.RTask notifications,
        TrialUsersMediatorClient trialUsersMediatorClient,
        MPlusTaskLauncherClient taskLauncherClient,
        OwnedRegisteredTasksCache cache)
        : base(trialUsersMediatorClient, taskLauncherClient, cache)
    {
        _notifications = notifications;
    }

    override internal async Task<OwnedRegisteredTask?> TryRegisterAsync(TaskCreationInfo taskInfo, TelegramBot.User taskOwner, CancellationToken cancellationToken)
    {
        if (await base.TryRegisterAsync(taskInfo, taskOwner, cancellationToken) is OwnedRegisteredTask ownedRegisteredTask)
        { await _notifications.SendResultPromiseAsyncFor(ownedRegisteredTask, cancellationToken); return ownedRegisteredTask; }
        else
        { await _notifications.SendRegistrationFailedAsyncFor(taskOwner.ChatId, taskInfo, cancellationToken); return null; }
    }
}
