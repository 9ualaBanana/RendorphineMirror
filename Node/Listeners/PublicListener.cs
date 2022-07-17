using System.Net;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class PublicListener : ExecutableListenerBase
{
    protected override bool IsLocal => false;
    protected override int Port => PortForwarding.Port;

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping")
            return await WriteText(response, $"ok from {MachineInfo.PCName} {MachineInfo.UserName} v{MachineInfo.Version}", HttpStatusCode.OK).ConfigureAwait(false);

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

                Log.Information($"Stopping torrent {hash}");
                await manager.StopAsync().ConfigureAwait(false);
                await TorrentClient.Client.RemoveAsync(manager).ConfigureAwait(false);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
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

                Log.Debug(@$"Downloading torrent {torrent.InfoHash.ToHex()} from peer {peerurl}");

                var peer = new Peer(BEncodedString.FromUrlEncodedString(peerid), new Uri("ipv4://" + peerurl));
                await manager.AddPeerAsync(peer).ConfigureAwait(false);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        return HttpStatusCode.NotFound;

    }
}
