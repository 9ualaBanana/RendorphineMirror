using System.Net;
using System.Text;

namespace Node.Listeners;

public abstract class ExecutableListenerBase : ListenerBase
{
    protected ExecutableListenerBase(ILogger logger) : base(logger) { }

    protected sealed override async ValueTask Execute(HttpListenerContext context)
    {
        var path = GetPath(context);

        var exec =
            context.Request.HttpMethod == "GET" ? await ExecuteGet(path, context)
            : context.Request.HttpMethod == "POST" ? await post(path, context)
            : context.Request.HttpMethod == "HEAD" ? await ExecuteHead(path, context)
            : HttpStatusCode.NotFound;

        context.Response.StatusCode = (int) exec;
        context.Response.Close();


        async Task<HttpStatusCode> post(string path, HttpListenerContext context)
        {
            var data = new byte[context.Request.ContentLength64];
            var span = data.AsMemory();

            while (true)
            {
                var read = await context.Request.InputStream.ReadAsync(span);
                if (read == 0) break;

                span = span.Slice(read);
            }

            try
            {
                var poststr = await new StreamReader(new MemoryStream(data)).ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(poststr))
                    Logger.Trace($"{context.Request.RemoteEndPoint} POST data:\n{poststr}");
            }
            catch { }

            return await ExecutePost(path, context, new MemoryStream(data));
        }
    }

    protected virtual Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context) => Task.FromResult(HttpStatusCode.NotFound);
    protected virtual Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context, Stream inputStream) => Task.FromResult(HttpStatusCode.NotFound);
    protected virtual Task<HttpStatusCode> ExecuteHead(string path, HttpListenerContext context) => Task.FromResult(HttpStatusCode.NotFound);
}
