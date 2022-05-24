global using Common;
using System.Net;



var listener = new HttpListener();
listener.Prefixes.Add("http://127.0.0.1:5000/");
listener.Start();

while (true)
{
    var context = await listener.GetContextAsync().ConfigureAwait(false);
    var request = context.Request;
    using var response = context.Response;
    using var writer = new StreamWriter(response.OutputStream);

    if (request.Url is null) continue;

    if (request.Url.AbsoluteUri.EndsWith("/ping"))
        response.StatusCode = (int) HttpStatusCode.OK;
}