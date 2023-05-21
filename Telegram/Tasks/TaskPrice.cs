using Telegram.MPlus.Clients;

namespace Telegram.Tasks;

public class TaskPrice
{
	readonly MPlusTaskLauncherClient _taskLauncherClient;

	public TaskPrice(MPlusTaskLauncherClient taskLauncherClient)
	{
		_taskLauncherClient = taskLauncherClient;
	}

	internal async Task<double> CalculateConsideringBonusBalanceAsyncFor(
		TaskAction taskAction,
		string sessionId,
		CancellationToken cancellationToken)
	{
        var prices = await _taskLauncherClient.RequestTaskPricesAsync(cancellationToken);
		var userBalance = await _taskLauncherClient.RequestBalanceAsync(sessionId, cancellationToken);

		return prices[taskAction] is var price && price <= userBalance.BonusBalance ?
			0 : price;
    }
}
