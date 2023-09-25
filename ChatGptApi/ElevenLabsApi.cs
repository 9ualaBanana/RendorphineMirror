namespace ChatGptApi;

public record ElevenLabsApi : ApiBase
{
    public const string Endpoint = "https://api.elevenlabs.io/v1";

    public ElevenLabsApi(HttpClient client, ILogger<ElevenLabsApi> logger) : base(client, logger) { }

    protected override bool NeedsToRetryRequest(OperationResult result) => false;

    public override async Task<OperationResult> ResponseToResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token)
    {
        await LogRequest(response, responseJson, errorDetails);

        var errmsg = responseJson?["detail"]?["message"]?.Value<string>();
        var ok = response.IsSuccessStatusCode;

        if (ok) return OperationResult.Succ();
        return OperationResult.Err(new HttpErrorBase(errmsg, response));
    }

    public override HttpContent ToPostContent((string, string)[] values) => ToPostContent(JObject.FromObject(values.ToDictionary(v => v.Item1, v => v.Item2)));
}
