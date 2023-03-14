namespace NodeToUI;

public record LocalApi(ApiInstance Api)
{
    public static readonly LocalApi Default = new LocalApi(Common.Api.Default);

    public LocalApi WithCancellationToken(CancellationToken token) => this with { Api = Api with { CancellationToken = token } };

    string Url(string part) => $"http://127.0.0.1:{NodeGlobalState.Instance.LocalListenPort}/{part}";

    public ValueTask<OperationResult<T>> Get<T>(string endpoint, string errorDetails, params (string, string)[] values) =>
        Get<T>(endpoint, "value", errorDetails, values);
    public ValueTask<OperationResult<T>> Post<T>(string endpoint, string errorDetails, params (string, string)[] values) =>
        Post<T>(endpoint, "value", errorDetails, values);
    public ValueTask<OperationResult<T>> Post<T>(string endpoint, string errorDetails, HttpContent content) =>
        Post<T>(endpoint, "value", errorDetails, content);


    public ValueTask<OperationResult<T>> Get<T>(string endpoint, string? property, string errorDetails, params (string, string)[] values) =>
        Api.ApiGet<T>(Url(endpoint), property, errorDetails, values);
    public ValueTask<OperationResult<T>> Post<T>(string endpoint, string? property, string errorDetails, params (string, string)[] values) =>
        Api.ApiPost<T>(Url(endpoint), property, errorDetails, values);
    public ValueTask<OperationResult<T>> Post<T>(string endpoint, string? property, string errorDetails, HttpContent content) =>
        Api.ApiPost<T>(Url(endpoint), property, errorDetails, content);

    public ValueTask<OperationResult> Get(string endpoint, string errorDetails, params (string, string)[] values) =>
        Api.ApiGet(Url(endpoint), errorDetails, values);
    public ValueTask<OperationResult> Post(string endpoint, string errorDetails, params (string, string)[] values) =>
        Api.ApiPost(Url(endpoint), errorDetails, values);
    public ValueTask<OperationResult> Post(string endpoint, string errorDetails, HttpContent content) =>
        Api.ApiPost(Url(endpoint), errorDetails, content);
}