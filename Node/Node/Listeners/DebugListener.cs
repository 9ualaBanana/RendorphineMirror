using System.Net;

namespace Node.Listeners;

public class DebugListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string Prefix => "debug";

    readonly PluginManager PluginManager;

    public DebugListener(PluginManager pluginManager, ILogger<DebugListener> logger) : base(logger) =>
        PluginManager = pluginManager;

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
        if (path == "addtask")
        {
            var task = new ReceivedTask("verylongtaskid", new TaskInfo(new TaskObject("debug.jpg", 798798), new MPlusTaskInputInfo("asd"), new MPlusTaskOutputInfo("a.mov", "dir"), new() { ["type"] = "EditVideo", ["hflip"] = true }, TaskPolicy.SameNode));
            NodeGlobalState.Instance.ExecutingTasks.Add(task);

            _ = Task.Delay(5000).ContinueWith(_ => NodeGlobalState.Instance.ExecutingTasks.Remove(task));

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
