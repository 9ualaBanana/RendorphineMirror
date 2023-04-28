using System.Globalization;

namespace Node.Tasks.Exec.Actions;

public abstract class FFMpegActionBase<T> : PluginAction<T>
{
    protected static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalDigits = 2,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };
    protected static readonly NumberFormatInfo NumberFormatNoDecimalLimit = new()
    {
        NumberDecimalDigits = 10,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };

    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.FFmpeg);

    protected delegate void ConstructFFMpegArgumentsDelegate(ITaskExecutionContext context, T data, FFMpegArgsHolder args);
    protected async Task ExecuteFFMpeg(ITaskExecutionContext context, T data, FileWithFormat file, TaskFileList outfiles, ConstructFFMpegArgumentsDelegate argfunc)
    {
        await FFMpegExec.ExecuteFFMpeg(context, file, args =>
        {
            argfunc(context, data, args);
            return outfiles.New(args.OutputFileFormat, args.OutputFileName).Path;
        });
    }
}
public abstract class FFMpegAction<T> : FFMpegActionBase<T>
{
    public sealed override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, T data)
    {
        foreach (var file in files.InputFiles)
            await ExecuteFFMpeg(context, data, file, files.OutputFiles.New(), ConstructFFMpegArguments);
    }

    protected abstract void ConstructFFMpegArguments(ITaskExecutionContext context, T data, FFMpegArgsHolder args);
}