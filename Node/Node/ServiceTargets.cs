using Node.Heartbeat;
using Node.Listeners;

namespace Node;

#pragma warning disable CA1707

/// <summary> A modular collection of classes to require to start the node. </summary>
public static class ServiceTargets
{
    public record BaseMain(UI UI, ReadyToExecuteTasks ReadyToExecuteTasks);

    public record DebugMain(BaseMain Base, Debug Debug);

    public record ReleaseMain(BaseMain Base, ConnectedToMPlus ConnectedToMPlus, PublicListeners PublicListeners, ReadyToReceiveTasks ReadyToReceiveTasks, AutoCleanup AutoCleanup);

    public record PublishMain(ReleaseMain Base, SystemTimerStartedTarget SystemTimerStartedTarget);


    /// <summary> Target to connect to the task server </summary>
    public record ConnectedToMPlus(ReconnectTarget _1, PortForwarder _2, MPlusHeartbeat _3, UserSettingsHeartbeat _4, TelegramBotHeartbeat _5);

    /// <summary> Target to enable Node.UI support </summary>
    public record UI(LocalListener _1, NodeStateListener _2, NodeGlobalStateInitializedTarget _3);

    public record PublicListeners(DownloadListener _1, PublicListener _2, PublicPagesListener _3, DirectoryDiffListener _4);

    public record ReadyToExecuteTasks(TaskHandler _1);
    public record ReadyToReceiveTasks(TaskReceiver _1, DirectUploadListener _2, DirectDownloadListener _3);

    public record Debug(DebugListener _1);
}
