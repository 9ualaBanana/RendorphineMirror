using System.Net;

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

            NodeGlobalState.Instance.ExecutingBenchmarks.Add("cpucpu", new() { ["rating"] = 123465789 });
            _ = Task.Delay(5000).ContinueWith(_ => NodeGlobalState.Instance.ExecutingBenchmarks.Remove("cpucpu"));

            return await WriteSuccess(response);
        }
        if (path == "addtask")
        {
            var task = new ReceivedTask("verylongtaskid", new TaskInfo("userid", 12456, new TaskObject("filename", 798798), new() { ["type"] = "MPlus" }, new() { ["type"] = "MPlusoutput" }, new() { ["type"] = "hflip" }));
            NodeGlobalState.Instance.ExecutingTasks.Add(task);

            _ = Task.Delay(5000).ContinueWith(_ => NodeGlobalState.Instance.ExecutingTasks.Remove(task));

            return await WriteSuccess(response);
        }

        return await base.ExecuteGet(path, context);
    }
}
