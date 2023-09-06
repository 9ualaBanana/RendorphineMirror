using Newtonsoft.Json.Linq;
using NodeToUI.Requests;

namespace NodeToUI;

public static class NodeGui
{
    public static readonly ImmutableDictionary<string, Type> GuiRequestTypes;
    public static readonly ImmutableDictionary<Type, string> GuiRequestNames;

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

    public static async Task<OperationResult<TResult>> Request<TResult>(GuiRequest request, CancellationToken token)
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

    public static ValueTask<OperationResult<string>> RequestCaptchaInputAsync(string base64image, CancellationToken token = default) => Request<string>(new CaptchaRequest(base64image), token);
}
