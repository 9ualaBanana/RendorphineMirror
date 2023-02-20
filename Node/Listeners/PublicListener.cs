using System.Net;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class PublicListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping")
            return await WriteJToken(response, $"ok from {MachineInfo.PCName} {MachineInfo.UserName} {Settings.NodeName} v{MachineInfo.Version} web{Settings.UPnpServerPort}").ConfigureAwait(false);

        if (path == "getcontents")
        {
            var authcheck = await CheckAuthentication(context).ConfigureAwait(false);
            if (!authcheck) return await WriteErr(response, "F");

            DirectoryContents contents;

            var dirpath = context.Request.QueryString["path"];
            if (dirpath is null || dirpath == string.Empty)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    contents = new DirectoryContents("", DriveInfo.GetDrives().Select(x => x.RootDirectory.FullName).ToImmutableArray());
                else contents = new DirectoryContents("/", Directory.GetDirectories("/").Select(x => Path.GetRelativePath("/", x)).ToImmutableArray());
            }
            else
            {
                if (!Directory.Exists(dirpath))
                    return await WriteErr(response, "Directory does not exists");

                contents = new DirectoryContents(dirpath, Directory.GetDirectories(dirpath).Select(x => Path.GetRelativePath(dirpath, x)).ToImmutableArray());
            }

            return await WriteJson(response, contents.AsOpResult());
        }
        if (path == "getfile")
        {
            var authcheck = await CheckAuthentication(context).ConfigureAwait(false);
            if (!authcheck) return await WriteErr(response, "F");

            var dirpath = context.Request.QueryString["path"];
            if (dirpath is null || dirpath == string.Empty) return await WriteErr(response, "No path");
            else
            {
                if (!File.Exists(dirpath))
                    return await WriteErr(response, "File does not exists");

                var temp = Path.GetTempFileName();
                using var _ = new FuncDispose(() => File.Delete(temp));

                File.Copy(dirpath, temp, true);
                using var reader = File.OpenRead(temp);
                await reader.CopyToAsync(response.OutputStream);
                return HttpStatusCode.OK;
            }
        }

        return HttpStatusCode.NotFound;
    }
}
