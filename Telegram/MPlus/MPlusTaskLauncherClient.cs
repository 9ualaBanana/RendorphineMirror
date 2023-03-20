using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using Telegram.Models;

namespace Telegram.MPlus;

public class MPlusTaskLauncherClient
{
	readonly HttpClient _client;

	readonly ILogger _logger;

	public MPlusTaskLauncherClient(HttpClient httpClient, ILogger<MPlusTaskLauncherClient> logger)
	{
		httpClient.BaseAddress = Api.TaskLauncherEndpoint;
		_client = httpClient;
		_logger = logger;
	}

	internal async Task<MPlusBalance?> TryRequestBalanceAsync(string sessionId, CancellationToken cancellationToken)
	{
		try { return await RequestBalanceAsync(sessionId, cancellationToken).ConfigureAwait(false); }
		catch { } { return null; }
	}

	internal async Task<MPlusBalance> RequestBalanceAsync(string sessionId, CancellationToken cancellationToken)
	{
		if ((await RequestBalanceAsyncCore()).ToObject<MPlusBalance>() is MPlusBalance balance)
			return balance;
		else
		{
			var exception = new InvalidDataException(LogId.Formatted("Balance request returned data in an unknown format.", sessionId, "SID"));
			_logger.LogCritical(exception, message: default);
			throw exception;
		}


		async Task<JToken> RequestBalanceAsyncCore()
		{
			string url = QueryHelpers.AddQueryString("getmybalance", "sessionid", sessionId);

            try { return await (await _client.GetAsync(url, cancellationToken)).GetJsonIfSuccessfulAsync(); }
			catch (Exception ex)
			{
				var exception = new Exception(LogId.Formatted("Balance request failed.", sessionId, "SID"), ex);
				_logger.LogError(exception, message: default);
				throw exception;
			}
		}
	}
}
