using Autofac;
using Microsoft.Extensions.Logging;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using Node.Common;


var builder = Init.CreateContainer(new Init.InitConfig("renderfin_tracker"), typeof(Program).Assembly);
using var container = builder.Build();
var logger = container.Resolve<ILogger<Program>>();


var tracker = new TrackerServer()
{
    AllowUnregisteredTorrents = true,
    MinAnnounceInterval = TimeSpan.FromSeconds(1)
};
tracker.PeerAnnounced += (obj, e) => /* */ logger.LogInformation($"PeerAnnounced {e.Torrent.Trackable.InfoHash.ToHex()} {e.Peer.ClientAddress}");
tracker.PeerTimedOut += (obj, e) => /*  */ logger.LogInformation($"PeerTimedOut  {e.Torrent.Trackable.InfoHash.ToHex()} {e.Peer.ClientAddress}");
tracker.PeerScraped += (obj, e) => /*   */ logger.LogInformation($"PeerScraped   {string.Join(", ", e.Torrents.Select(x => x.Trackable.InfoHash.ToHex()))}");

const int porth = 5120;
var listenerh = TrackerListenerFactory.CreateHttp($"http://t.microstock.plus:{porth}/announce/");
tracker.RegisterListener(listenerh);
listenerh.Start();

const int portu = 5121;
var listeneru = TrackerListenerFactory.CreateUdp(portu);
tracker.RegisterListener(listeneru);
listeneru.Start();

logger.LogInformation("Tracker started on port http " + porth + " and udp " + portu);
Thread.Sleep(-1);
