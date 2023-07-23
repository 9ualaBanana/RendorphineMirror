using System.Net;
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

    internal async Task<OwnedRegisteredTask?> TryRegisterAsync(TaskCreationInfo taskInfo, TelegramBot.User taskOwner, string sessionId)
    {
        var response = await _trialUsersMediatorClient.TryReduceQuotaAsync(taskInfo.Action, taskOwner.ChatId!, MPlusIdentity.UserIdOf(taskOwner._));
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized)
        {
            var registeredTask = (await TaskRegistration.RegisterAsync(taskInfo, sessionId)).Result;

            var ownedRegisteredTask = registeredTask.OwnedBy(taskOwner);
            _cache.Add(ownedRegisteredTask);

            return ownedRegisteredTask;
        }
        else return null;
    }
}
