using System.Net;

namespace Node.Listeners;

public abstract class ExecutableListenerBase : ListenerBase
{
    protected sealed override async ValueTask Execute(HttpListenerContext context)
    {
        if (context.Request.Url is null) return;

        var response = context.Response;

        var path = context.Request.Url.LocalPath.Substring((Prefix?.Length ?? 0) + 1);
        if (path[0] == '/') path = path[1..];
        if (path[^1] == '/') path = path[..^1];

        var exec =
            context.Request.HttpMethod == "GET" ? await ExecuteGet(path, context)
            : context.Request.HttpMethod == "POST" ? await ExecutePost(path, context)
            : HttpStatusCode.NotFound;

        context.Response.StatusCode = (int) exec;
        context.Response.Close();
    }

    protected virtual Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context) => Task.FromResult(HttpStatusCode.NotFound);
    protected virtual Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context) => Task.FromResult(HttpStatusCode.NotFound);
}
