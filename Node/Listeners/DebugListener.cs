using System.Net;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class DebugListener : ExecutableListenerBase
{
    protected override string Prefix => "debug";

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
            var task = new ReceivedTask("verylongtaskid", new TaskInfo(new TaskObject("filename", 798798), new() { ["type"] = "MPlus" }, new() { ["type"] = "MPlusoutput" }, new() { ["type"] = "hflip" }), true);
            NodeGlobalState.Instance.ExecutingTasks.Add(task);

            _ = Task.Delay(5000).ContinueWith(_ => NodeGlobalState.Instance.ExecutingTasks.Remove(task));

            return await WriteSuccess(response);
        }

        if (path == "getcfg")
        {
            var cfg = new JObject();
            foreach (var setting in Settings.Bindables)
                cfg[setting.Name] = setting.ToJson();

            return await WriteJToken(response, cfg).ConfigureAwait(false);
        }
        if (path == "login")
        {
            return await Test(context.Request, response, "login", "password", async (login, password) =>
            {
                var auth = await SessionManager.AuthAsync(login, password);
                if (!auth) return await WriteJson(response, auth);

                return await WriteJson(response, Settings.SessionId!.AsOpResult());
            });
        }

        return await base.ExecuteGet(path, context);
    }
}
