using System.IO.Compression;
using System.Net;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class PublicListener : ExecutableListenerBase
{
    protected override bool IsLocal => false;

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping")
            return await WriteJToken(response, $"ok from {MachineInfo.PCName} {MachineInfo.UserName} v{MachineInfo.Version}").ConfigureAwait(false);

        if (path == "torrentinfo")
        {
            return await Test(request, response, "hash", async hash =>
            {
                var ihash = InfoHash.FromHex(hash);
                var manager = TorrentClient.TryGet(ihash);
                if (manager is null) return await WriteErr(response, "no such torrent").ConfigureAwait(false);

                var data = new JObject()
                {
                    ["peers"] = JObject.FromObject(manager.Peers),
                    ["progress"] = new JValue(manager.PartialProgress),
                    ["monitor"] = JObject.FromObject(manager.Monitor),
                };

                return await WriteJToken(response, data).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        if (path == "stoptorrent")
        {
            return await Test(request, response, "hash", async hash =>
            {
                var ihash = InfoHash.FromHex(hash);
                var manager = TorrentClient.TryGet(ihash);
                if (manager is null) return await WriteErr(response, "no such torrent").ConfigureAwait(false);

                _logger.Info("Stopping torrent {Hash}", hash);
                await manager.StopAsync().ConfigureAwait(false);
                await TorrentClient.Client.RemoveAsync(manager).ConfigureAwait(false);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

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

        if (path == "helloworld")
        {
            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            writer.Write("Hello world, epta");

            return HttpStatusCode.OK;
        }

        if (path == "logs")
        {
            string logDir = Init.LogDirectory;
            string[] folders = Directory.GetDirectories(logDir);
            string? q = context.Request.QueryString["id"];
            string info = "";

            if (q == null || !int.TryParse(q, out _))
            {
                int i = 0;
                foreach (string folder in folders)
                {
                    string[] files = Directory.GetFiles(folder);
                    info += $"<b style='font-size: 32px'>{Path.GetFileName(folder)}</b></br>";
                    foreach (string file in files)
                    {
                        info += $"<a href='/logs?id={i++}'>{Path.GetFileName(file)}</a></br>";
                    }
                }
            }
            else
            {
                int i = 0;
                int id = int.Parse(q);
                foreach(string folder in folders)
                {
                    string[] files = Directory.GetFiles(folder);
                    if(i + files.Length - 1 >= id)
                    {
                        info = File.ReadAllText(files[id - i]);
                        break;
                    }
                    i += files.Length;
                }
            }

            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            writer.Write(info);

            return HttpStatusCode.OK;
        }

        if (path.StartsWith("gallery"))
        {
            string[] fileExt = { "jpg", "jpeg", "png" };
            string taskDir = Init.TaskFilesDirectory;
            

            if (path == "gallery")
            {
                IEnumerable<string> foldersWithOutputs = Directory.GetDirectories(taskDir)
                .Where(d => Directory.Exists(d + "/output") && Directory.EnumerateFiles(d + "/output").Any());
                var tasksFiles = foldersWithOutputs
                    .GroupBy(folder => fileExt.SelectMany(ext => Directory.EnumerateFiles(folder + "/output", "*." + ext)));

                string info = "<html><body>";

                foreach (var task in tasksFiles)
                {
                    info += $"<br><h3>{Path.GetFileName(task.First())}</h3>";
                    foreach (string file in task.Key)
                    {
                        info += $"<img width='200px' src='{Path.GetFileName(task.First()) + "?name=" + Path.GetFileName(file)}'>";
                    }
                }

                info += "</body></html>";

                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write(info);
                return HttpStatusCode.OK;
            }
            else
            {
                string name = context.Request.QueryString["name"] ?? "";
                string file = taskDir + "/" + Path.GetFileName(Path.GetFileName(path)) + "/output/" + name;
                if (File.Exists(file))
                {
                    using var writer = new BinaryWriter(response.OutputStream, System.Text.Encoding.Default, leaveOpen: true);
                    writer.Write(File.ReadAllBytes(file));
                    return HttpStatusCode.OK;
                }
            }
        }

        if (path == "")
        {
            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            writer.Write("WE'LL FUCK THIS WORLD!");
            return HttpStatusCode.OK;
        }

        return HttpStatusCode.NotFound;
    }
    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var query = request.QueryString;

        if (path == "downloadtorrent")
        {
            return await Test(request, response, "peerurl", "peerid", async (peerurl, peerid) =>
            {
                // using memorystream since request.InputStream doesnt support seeking
                var stream = new MemoryStream();
                await request.InputStream.CopyToAsync(stream).ConfigureAwait(false);
                stream.Position = 0;

                var torrent = await Torrent.LoadAsync(stream).ConfigureAwait(false);
                var manager = await TorrentClient.AddOrGetTorrent(torrent, "torrenttest_" + torrent.InfoHash.ToHex()).ConfigureAwait(false);

                _logger.Debug(@$"Downloading torrent {torrent.InfoHash.ToHex()} from peer {peerurl}");

                var peer = new Peer(BEncodedString.FromUrlEncodedString(peerid), new Uri("ipv4://" + peerurl));
                await manager.AddPeerAsync(peer).ConfigureAwait(false);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        return HttpStatusCode.NotFound;
    }
}
