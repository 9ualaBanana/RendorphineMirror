using System.Net;

namespace Node.Listeners;

public class DebugListener : ExecutableListenerBase
{
    protected override string Prefix => "debug";

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var response = context.Response;

        if (path == "setstate")
        {
            var query = context.Request.QueryString;
            var state = query["state"];

            if (state == "idle") GlobalState.State = IdleNodeState.Instance;
            else if (state == "benchmark") GlobalState.State = new BenchmarkNodeState() { Completed = { "sex" } };
            else if (state == "task") GlobalState.State = new ExecutingTaskNodeState(new TaskInfo("userid", 012, new TaskObject("FN.exe", 123), new(), new(), new() { ["bri"] = 9998 }));
            else return await WriteJson(response, OperationResult.Err("unknown state"));

            return await WriteSuccess(response);
        }

        return await base.ExecuteGet(path, context);
    }
}
