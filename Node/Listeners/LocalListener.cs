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
        if (path == "setcfg")
        {
            return await Test(request, response, "key", "value", async (key, value) =>
            {
                var bindable = Settings.Bindables.FirstOrDefault(x => x.Name == key);
                if (bindable is null) return await WriteErr(response, "no such cfg").ConfigureAwait(false);

                try { bindable.SetFromJson(value); }
                catch (Exception ex) { return await WriteErr(response, "invalid value: " + ex.Message).ConfigureAwait(false); }

                return await writeConfig().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        if (path == "getcfg") return await writeConfig().ConfigureAwait(false);

        if (path == "getactions")
        {
            var actions = TaskList.Actions.Select(serialize).ToImmutableArray();
            var inputs = new[]
            {
                serializeinout<MPlusTaskInputInfo>(TaskInputOutputType.MPlus),
                serializeinout<MPlusTaskInputInfo>(TaskInputOutputType.User),
            }.ToImmutableArray();
            var outputs = new[]
            {
                serializeinout<MPlusTaskOutputInfo>(TaskInputOutputType.MPlus),
                serializeinout<UserTaskOutputInfo>(TaskInputOutputType.User),
            }.ToImmutableArray();
            var repeats = new[]
            {
                serializerep<MPlusRepeatingTaskSource>(TaskInputOutputType.MPlus),
                serializerep<LocalRepeatingTaskSource>(TaskInputOutputType.User),
            }.ToImmutableArray();

            var output = new TasksFullDescriber(actions, inputs, outputs, repeats);
            return await WriteJToken(response, JToken.FromObject(output, JsonSerializerWithTypes)).ConfigureAwait(false);


            static TaskActionDescriber serialize(IPluginAction action) => new TaskActionDescriber(action.Type, action.Name, (ObjectDescriber) FieldDescriber.Create(action.DataType));
            static TaskInputOutputDescriber serializeinout<T>(TaskInputOutputType type) where T : ITaskInputOutputInfo => new TaskInputOutputDescriber(type.ToString(), (ObjectDescriber) FieldDescriber.Create(typeof(T)));
            static TaskInputOutputDescriber serializerep<T>(TaskInputOutputType type) where T : IRepeatingTaskSource => new TaskInputOutputDescriber(type.ToString(), (ObjectDescriber) FieldDescriber.Create(typeof(T)));
        }

        return HttpStatusCode.NotFound;


        async Task<HttpStatusCode> writeConfig()
        {
            var cfg = new JObject();
            foreach (var setting in Settings.Bindables)
                if (!setting.Hidden)
                    cfg[setting.Name] = setting.ToJson();

            return await WriteJToken(response, cfg).ConfigureAwait(false);
        }
    }

    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "starttask")
        {
            var task = new Newtonsoft.Json.JsonSerializer().Deserialize<TaskCreationInfo>(new JsonTextReader(new StreamReader(request.InputStream)))!;
            var taskid = await NodeTask.RegisterAsync(task).ConfigureAwait(false);

            return await WriteJson(response, taskid).ConfigureAwait(false);
        }

        return HttpStatusCode.NotFound;
    }
}
