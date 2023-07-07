using Telegram.Infrastructure.Bot;
using Telegram.MPlus.Security;
using Telegram.TrialUsers;

namespace Telegram.Infrastructure.Tasks;

public class TaskManager
{
    readonly TrialUsersMediatorClient _trialUsersMediatorClient;
    readonly OwnedRegisteredTasksCache _cache;

    public TaskManager(TrialUsersMediatorClient trialUsersMediatorClient, OwnedRegisteredTasksCache cache)
    {
        _trialUsersMediatorClient = trialUsersMediatorClient;
        _cache = cache;
    }

    internal async Task<OwnedRegisteredTask> RegisterAsync(TaskCreationInfo taskInfo, TelegramBot.User user, string sessionId)
    {
        var registeredTask = (await TaskRegistration.RegisterAsync(taskInfo, sessionId)).Result;
        // Reduces quota of a trial user if `ChatId` belongs to an authenticated trial user.
        await _trialUsersMediatorClient.TryReduceQuotaAsync(taskInfo.Action, user.ChatId, MPlusIdentity.UserIdOf(user._));
        var ownedRegisteredTask = registeredTask.OwnedBy(user);

        _cache.Add(ownedRegisteredTask);

        return ownedRegisteredTask;
    }
}
