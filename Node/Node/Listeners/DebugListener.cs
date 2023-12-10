using System.Net;
using Node.Tasks.Watching.Handlers.Input;

namespace Node.Listeners;

public class DebugListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string Prefix => "debug";

    public required SessionManager SessionManager { get; init; }
    public required ILifetimeScope Container { get; init; }

    public DebugListener(ILogger<DebugListener> logger) : base(logger) { }

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var response = context.Response;

        if (path == "addbench")
        {
            var query = context.Request.QueryString;

            NodeGlobalState.Instance.ExecutingBenchmarks.Add("cpucpu", new JObject() { ["rating"] = 123465789 });
            _ = Task.Delay(5000).ContinueWith(_ => NodeGlobalState.Instance.ExecutingBenchmarks.Remove("cpucpu"));

            return await WriteSuccess(response);
        }

        if (path == "login")
        {
            var loginr = ReadQueryString(context.Request.QueryString, "login");
            if (!loginr) return await WriteJson(response, loginr);
            var login = loginr.Value;

            var passwordr = ReadQueryString(context.Request.QueryString, "password");
            var password = null as string;
            if (passwordr) password = passwordr.Value;

            var auth = password is null
                ? await SessionManager.AutoAuthAsync(login)
                : await SessionManager.AuthAsync(login, password);
            if (!auth) return await WriteJson(response, auth);

            return await WriteJson(response, Settings.SessionId!.AsOpResult());
        }

        return await base.ExecuteGet(path, context);
    }
}
