using Node.Heartbeat;
using Node.Listeners;

namespace Node;

/// <summary> A modular collection of classes to require to start the node. </summary>
public static class ServiceTargets
{
    public class BaseMain
    {
        public required UI UI { get; init; }
        public required ReadyToExecuteTasks ReadyToExecuteTasks { get; init; }
    }
    public class DebugMain
    {
        public required BaseMain Base { get; init; }
        public required Debug Debug { get; init; }
    }
    public class ReleaseMain
    {
        public required BaseMain Base { get; init; }
        public required ConnectedToMPlus ConnectedToMPlus { get; init; }
        public required PublicListeners PublicListeners { get; init; }
        public required ReadyToReceiveTasks ReadyToReceiveTasks { get; init; }

        public required AutoCleanup AutoCleanup { get; init; }
    }
    public class PublishMain
    {
        public required ReleaseMain Base { get; init; }

        public required SystemTimerStartedTarget SystemTimerStartedTarget { get; init; }
    }


    /// <summary> Target to connect to the task server </summary>
    public class ConnectedToMPlus
    {
        public ConnectedToMPlus(ReconnectTarget _1, PortForwarder _2, MPlusHeartbeat _3, UserSettingsHeartbeat _4, TelegramBotHeartbeat _5) { }
    }

    /// <summary> Target to enable Node.UI support </summary>
    public class UI
    {
        public UI(NodeStateListener _1, LocalListener _2, NodeGlobalStateInitializedTarget _3) { }
    }

    public class PublicListeners
    {
        public PublicListeners(DownloadListener _1, PublicListener _2, PublicPagesListener _3, DirectoryDiffListener _4) { }
    }

    public class ReadyToExecuteTasks
    {
        public ReadyToExecuteTasks(TaskHandler2 _1) { }
    }
    public class ReadyToReceiveTasks
    {
        public ReadyToReceiveTasks(TaskReceiver _1, DirectUploadListener _2, DirectDownloadListener _3) { }
    }

    public class Debug
    {
        public Debug(DebugListener _1) { }
    }
}
