using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Node.Plugins.Deployment;
using Node.Profiling;

namespace Node.Listeners;

public class LocalListener : ExecutableListenerBase
{
    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping") return HttpStatusCode.OK;

        var query = request.QueryString;

        if (path == "uploadtorrent")
        {
            return await Test(request, response, "url", "dir", async (url, dir) =>
            {
                var peerid = TorrentClient.PeerId.UrlEncode();
                var peerurl = PortForwarding.GetPublicIPAsync().ConfigureAwait(false);
                var (data, manager) = await TorrentClient.CreateAddTorrent(dir).ConfigureAwait(false);
                var downloadr = await LocalApi.Post(url, $"downloadtorrent?peerid={peerid}&peerurl={await peerurl}:{TorrentClient.ListenPort}", new ByteArrayContent(data)).ConfigureAwait(false);
                if (!downloadr) return await WriteJson(response, downloadr).ConfigureAwait(false);

                return await WriteJson(response, manager.InfoHash.ToHex().AsOpResult()).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "reloadcfg")
        {
            Settings.Reload();
            return await WriteSuccess(response).ConfigureAwait(false);
        }

        if (path == "setnick")
        {
            return await Test(request, response, "nick", async nick =>
            {
                OperationResult resp;
                lock (Profiler.HeartbeatLock)
                {
                    resp = SessionManager.RenameServerAsync(nick).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (resp) Settings.NodeName = nick;
                }

                return await WriteJson(response, resp).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "deploy")
        {
            return await Test(request, response, "type", "version", async (type, version) =>
            {
                new ScriptPluginDeploymentInfo(new PluginToDeploy() { Type = Enum.Parse<PluginType>(type, true), Version = version }).DeployAsync().Consume();
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
}
