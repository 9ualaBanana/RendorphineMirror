using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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


        return HttpStatusCode.NotFound;
    }

    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "starttask")
        {
            var task = new Newtonsoft.Json.JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;
            var taskid = await TaskRegistration.RegisterAsync(task);

            return await WriteJson(response, taskid).ConfigureAwait(false);
        }

        if (path == "startwatchingtask")
        {
            var task = new Newtonsoft.Json.JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;

            var wt = new WatchingTask(task.Input.ToObject<IWatchingTaskSource>(LocalApi.JsonSerializerWithType)!, task.Action, task.Data, task.Output.ToObject<IWatchingTaskOutputInfo>(LocalApi.JsonSerializerWithType)!, task.Policy);
            wt.StartWatcher();
            NodeSettings.WatchingTasks.Bindable.Add(wt);

            return await WriteJson(response, wt.Id.AsOpResult()).ConfigureAwait(false);
        }

        return HttpStatusCode.NotFound;
    }
}
