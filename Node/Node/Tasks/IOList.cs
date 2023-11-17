using Node.Tasks.IO.Handlers.Input;
using Node.Tasks.IO.Handlers.Output;
using Node.Tasks.Watching.Handlers.Input;
using IN = Node.Tasks.IO.Handlers.Input;
using IWatchingTaskInputHandler = Node.Tasks.Watching.Input.IWatchingTaskInputHandler;
using OUT = Node.Tasks.IO.Handlers.Output;

namespace Node.Tasks;

public static class IOList
{
    static void Register<TType, TInterface>(this ContainerBuilder builder, object type)
        where TType : TInterface
        where TInterface : notnull =>
        builder.RegisterType<TType>()
            .Keyed<TInterface>(type)
            .InstancePerDependency();

    static void RegisterInput<TID, TOP>(this ContainerBuilder builder)
        where TID : ITaskInputDownloader, ITypedTaskInput
        where TOP : ITaskObjectProvider, ITypedTaskInput
    {
        builder.Register<TID, ITaskInputDownloader>(TID.Type);
        builder.Register<TOP, ITaskObjectProvider>(TOP.Type);
    }

    static void RegisterInput<TID, TOP, TIU>(this ContainerBuilder builder)
        where TID : ITaskInputDownloader, ITypedTaskInput
        where TOP : ITaskObjectProvider, ITypedTaskInput
        where TIU : ITaskInputUploader, ITypedTaskInput
    {
        builder.RegisterInput<TID, TOP>();
        builder.Register<TIU, ITaskInputUploader>(TIU.Type);
    }

    static void RegisterOutput<TUH>(this ContainerBuilder builder)
        where TUH : ITaskUploadHandler, ITypedTaskOutput
    {
        builder.Register<TUH, ITaskUploadHandler>(TUH.Type);
    }

    static void RegisterOutput<TUH, TCC>(this ContainerBuilder builder)
        where TUH : ITaskUploadHandler, ITypedTaskOutput
        where TCC : ITaskCompletionChecker, ITypedTaskOutput
    {
        builder.RegisterOutput<TUH>();
        builder.Register<TCC, ITaskCompletionChecker>(TCC.Type);
    }

    static void RegisterOutput<TUH, TCC, TCH>(this ContainerBuilder builder)
        where TUH : ITaskUploadHandler, ITypedTaskOutput
        where TCC : ITaskCompletionChecker, ITypedTaskOutput
        where TCH : ITaskCompletionHandler, ITypedTaskOutput
    {
        builder.RegisterOutput<TUH, TCC>();
        builder.Register<TCH, ITaskCompletionHandler>(TCH.Type);
    }

    static void RegisterWatchingInput<T>(this ContainerBuilder builder)
        where T : IWatchingTaskInputHandler, ITypedTaskWatchingInput
    {
        builder.Register<T, IWatchingTaskInputHandler>(T.Type);
    }


    public static void RegisterAll(ContainerBuilder builder)
    {
        builder.RegisterInput<DirectUpload.InputDownloader, DirectUpload.TaskObjectProvider, DirectUpload.InputUploader>();
        builder.RegisterInput<DownloadLink.InputDownloader, DownloadLink.TaskObjectProvider>();
        builder.RegisterInput<IN.MPlus.InputDownloader, IN.MPlus.TaskObjectProvider>();
        builder.RegisterInput<Stub.InputDownloader, Stub.TaskObjectProvider>();
        builder.RegisterInput<IN.TitleKeywords.InputDownloader, IN.TitleKeywords.TaskObjectProvider>();
        builder.RegisterInput<IN.Torrent.InputDownloader, IN.Torrent.TaskObjectProvider, IN.Torrent.InputUploader>();

        builder.RegisterOutput<DirectDownload.UploadHandler, DirectDownload.CompletionChecker, DirectDownload.CompletionHandler>();
        builder.RegisterOutput<OUT.MPlus.UploadHandler, OUT.MPlus.CompletionChecker>();
        builder.RegisterOutput<QSPreview.UploadHandler, QSPreview.CompletionChecker>();
        builder.RegisterOutput<OUT.TitleKeywords.UploadHandler>();
        builder.RegisterOutput<OUT.Torrent.UploadHandler, OUT.Torrent.CompletionChecker, OUT.Torrent.CompletionHandler>();

        builder.RegisterWatchingInput<LocalWatchingTaskInputHandler>();
        builder.RegisterWatchingInput<MPlusWatchingTaskHandler>();
        builder.RegisterWatchingInput<QSWatchingTaskHandler>();
        builder.RegisterWatchingInput<OtherUserWatchingTaskHandler>();
        builder.RegisterWatchingInput<RectReleasesWatchingTaskHandler>();
        builder.RegisterWatchingInput<OneClickWatchingTaskInputHandler>();
    }
}
