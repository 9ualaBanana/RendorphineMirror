using System.Net;
using System.Web;

namespace Transport.Models;

internal record DownloadFileInfo(
    string SessionId,
    string Name,
    long Size,
    string Extension)
{
    internal static async Task<DownloadFileInfo> DeserializeAsync(HttpListenerRequest request)
    {
        var queryArgs = HttpUtility.ParseQueryString(
            await new StreamReader(request.InputStream).ReadToEndAsync());
        return new(
            queryArgs["sessionid"]!,
            queryArgs["name"]!,
            long.Parse(queryArgs["size"]!),
            queryArgs["extension"]!);
    }
}
