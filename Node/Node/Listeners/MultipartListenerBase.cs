using System.Net;

namespace Node.Listeners;

public abstract class MultipartListenerBase : ListenerBase
{
    protected MultipartListenerBase(ILogger logger) : base(logger) { }

    protected sealed override async ValueTask Execute(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "POST") return;

        var contenttype = context.Request.ContentType;
        if (contenttype is null || !contenttype.Contains("multipart/form-data", StringComparison.Ordinal))
            return;

        var boundary = contenttype.Split("boundary=")[1][1..^2];
        using var reader = await CachedMultipartReader.Create(boundary, context.Request.InputStream);

        var exec = await Execute(context, reader);
        context.Response.StatusCode = (int) exec;
        context.Response.Close();
    }

    protected abstract ValueTask<HttpStatusCode> Execute(HttpListenerContext context, CachedMultipartReader sections);
}
