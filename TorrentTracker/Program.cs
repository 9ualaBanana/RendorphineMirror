using Common;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using NLog;

Initializer.ConfigDirectory = "renderphin_tracker";
Init.Initialize();

var logger = LogManager.GetCurrentClassLogger();


var tracker = new TrackerServer();

tracker.PeerAnnounced += (obj, e) => logger.Info($"PeerAnnounced {e.Peer}");
tracker.PeerTimedOut += (obj, e) => logger.Info($"PeerTimedOut {e.Peer}");

const int port = 5120;
var listener = TrackerListenerFactory.CreateHttp(port);
tracker.RegisterListener(listener);
listener.Start();

logger.Info("Tracker started on port " + port);



Thread.Sleep(-1);