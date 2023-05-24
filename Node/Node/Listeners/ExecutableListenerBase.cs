using System.Net;

namespace Node.Listeners;

public abstract class ExecutableListenerBase : ListenerBase
{
    protected sealed override async ValueTask Execute(HttpListenerContext context)
    {
        var path = GetPath(context);

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
