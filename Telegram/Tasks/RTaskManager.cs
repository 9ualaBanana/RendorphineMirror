using System.Net;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;
using Telegram.TrialUsers;

namespace Telegram.Tasks;

public class RTaskManager
{
    readonly TrialUsersMediatorClient _trialUsersMediatorClient;
    readonly MPlusTaskLauncherClient _taskLauncherClient;
    readonly OwnedRegisteredTasksCache _cache;

    public RTaskManager(TrialUsersMediatorClient trialUsersMediatorClient, MPlusTaskLauncherClient taskLauncherClient, OwnedRegisteredTasksCache cache)
    {
        _trialUsersMediatorClient = trialUsersMediatorClient;
        _taskLauncherClient = taskLauncherClient;
        _cache = cache;
    }

    virtual internal async Task<OwnedRegisteredTask?> TryRegisterAsync(TaskCreationInfo rTaskInfo, TelegramBot.User rTaskOwner, CancellationToken cancellationToken)
    {
        var response = await _trialUsersMediatorClient.TryReduceQuotaAsync(rTaskInfo.Action, rTaskOwner.ChatId!, MPlusIdentity.UserIdOf(rTaskOwner), cancellationToken);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized)
        {
            var registeredTask = (await TaskRegistration.RegisterAsync(rTaskInfo, MPlusIdentity.SessionIdOf(rTaskOwner))).Result;

            var ownedRegisteredTask = registeredTask.OwnedBy(rTaskOwner);
            _cache.Add(ownedRegisteredTask);

            return ownedRegisteredTask;
        }
        else return null;
    }

    internal async Task<double> CalculatePriceAsyncFor(TaskAction rTaskAction, string sessionId, CancellationToken cancellationToken)
    {
        var prices = await _taskLauncherClient.RequestTaskPricesAsync(cancellationToken);
        var userBalance = await _taskLauncherClient.RequestBalanceAsync(sessionId, cancellationToken);

        return prices[rTaskAction] is var price && price <= userBalance.BonusBalance ?
            0 : price;
    }
}
