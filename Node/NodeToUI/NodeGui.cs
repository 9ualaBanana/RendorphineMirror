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

        bool completed = false;
        JToken? result = null;
        task.Task.ContinueWith(t =>
        {
            result = t.Result;
            completed = true;
        }, token).Consume();

        using var _ = new FuncDispose(() => NodeGlobalState.Instance.Requests.Remove(reqid));
        NodeGlobalState.Instance.Requests.Add(reqid, request);

        while (!completed)
        {
            await Task.Delay(500, token);
            token.ThrowIfCancellationRequested();
        }

        return result.ThrowIfNull().ToObject<TResult>()!;
    }

    public Task<OperationResult<string>> RequestCaptchaInputAsync(string base64image, CancellationToken token = default) => Request<string>(new CaptchaRequest(base64image), token);
}
