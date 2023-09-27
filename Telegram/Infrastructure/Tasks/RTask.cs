using System.Net;
using Telegram.Infrastructure.Bot;
using Telegram.MPlus.Security;
using Telegram.TrialUsers;

namespace Telegram.Infrastructure.Tasks;

public class RTask
{
    readonly TrialUsersMediatorClient _trialUsersMediatorClient;
    readonly OwnedRegisteredTasksCache _cache;

    public RTask(TrialUsersMediatorClient trialUsersMediatorClient, OwnedRegisteredTasksCache cache)
    {
        _trialUsersMediatorClient = trialUsersMediatorClient;
        _cache = cache;
    }

    internal async Task<OwnedRegisteredTask?> TryRegisterAsync(TaskCreationInfo rTaskInfo, TelegramBot.User rTaskOwner)
    {
        var response = await _trialUsersMediatorClient.TryReduceQuotaAsync(rTaskInfo.Action, rTaskOwner.ChatId!, MPlusIdentity.UserIdOf(rTaskOwner));
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized)
        {
            var registeredTask = (await TaskRegistration.RegisterAsync(rTaskInfo, MPlusIdentity.SessionIdOf(rTaskOwner))).Result;

            var ownedRegisteredTask = registeredTask.OwnedBy(rTaskOwner);
            _cache.Add(ownedRegisteredTask);

            return ownedRegisteredTask;
        }
        else return null;
    }
}
