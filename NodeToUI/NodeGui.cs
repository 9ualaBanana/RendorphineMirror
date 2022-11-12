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
        }.ToImmutableDictionary();

        GuiRequestNames = GuiRequestTypes.ToImmutableDictionary(x => x.Value, x => x.Key);
    }

    public static async ValueTask<OperationResult<TResponse>> Request<TResponse>(GuiRequest request)
    {
        var reqid = Guid.NewGuid().ToString() + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var task = new TaskCompletionSource<JToken>();
        request.Task = task;

        using var _ = new FuncDispose(() => NodeGlobalState.Instance.Requests.Remove(reqid));
        NodeGlobalState.Instance.Requests.Add(reqid, request);

        var result = await task.Task;
        return result.ToObject<TResponse>()!;
    }

    public static ValueTask<OperationResult<string>> RequestCaptchaInput(string base64image) => Request<string>(new CaptchaRequest(base64image));
}
