namespace NodeToUI;

public class NodeGui : INodeGui
{
    public static ImmutableDictionary<string, Type> GuiRequestTypes { get; }
    public static ImmutableDictionary<Type, string> GuiRequestNames { get; }

    static NodeGui()
    {
        GuiRequestTypes = new Dictionary<string, Type>()
        {
            ["captcharesponse"] = typeof(CaptchaRequest),
            ["inputresponse"] = typeof(InputRequest),
            ["retryorskipresponse"] = typeof(RetryOrSkipRequest),
            ["tsmiresponse"] = typeof(InputTurboSquidModelInfoRequest),
        }.ToImmutableDictionary();

        GuiRequestNames = GuiRequestTypes.ToImmutableDictionary(x => x.Value, x => x.Key);
    }

    public async Task<OperationResult<TResult>> Request<TResult>(GuiRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var reqid = Guid.NewGuid().ToString() + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var task = new TaskCompletionSource<JToken>();
        request.Task = task;

        using var _ = new FuncDispose(() => NodeGlobalState.Instance.Requests.Remove(reqid));
        NodeGlobalState.Instance.Requests.Add(reqid, request);

        return (await task.Task).ToObject<TResult>().ThrowIfNull();
    }

    public Task<OperationResult<string>> RequestCaptchaInputAsync(string base64image, CancellationToken token = default) => Request<string>(new CaptchaRequest(base64image), token);
}
