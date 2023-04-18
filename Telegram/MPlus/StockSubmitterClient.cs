using Newtonsoft.Json.Linq;

namespace Telegram.MPlus;

public class StockSubmitterClient
{
	readonly HttpClient _httpClient;

	readonly ILogger _logger;

	public StockSubmitterClient(HttpClient httpClient, ILogger<StockSubmitterClient> logger)
	{
		_httpClient = httpClient;
		_httpClient.BaseAddress = new("https://accounts.stocksubmitter.com/api/0.1/");
		_logger = logger;
	}

	internal async Task<string> TranslateAsync(string token, string userId, CancellationToken cancellationToken)
		=> (await TranslateAsync(new string[] { token }, userId, cancellationToken)).Single();

	internal async Task<IEnumerable<string>> TranslateAsync(IEnumerable<string> tokens, string userId, CancellationToken cancellationToken)
	{
        const string Service = "yandex";

        var request = new HttpRequestMessage(HttpMethod.Post, "common/translator/dictionarylookup")
		{
			Content = new FormUrlEncodedContent(
				new Dictionary<string, string>()
				{
					["userid"] = userId,
					["service"] = Service,
					["data"] = $"[{string.Join(',', tokens.Select(token => $"\"{token}\""))}]"
				})
		};
		var response = JObject.Parse(await
			(await _httpClient.SendAsync(request, cancellationToken))
			.Content.ReadAsStringAsync(cancellationToken));

		if ((bool)response["isOk"]!)
			return response["translations"]!.Children()
				.Select(tokenTranslations => tokenTranslations.Values().First()["direct"]!.Value<string>()!);
		else
		{
			var exception = new HttpRequestException($"Internal status code didn't indicate success ({(int)response["errorCode"]!})");
			_logger.LogCritical(exception, message: default);
			throw exception;
		}
	}
}
