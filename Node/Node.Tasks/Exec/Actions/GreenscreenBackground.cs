namespace Node.Tasks.Exec.Actions;

public class GreenscreenBackground : PluginAction<GreenscreenBackgroundInfo>
{
    public override TaskAction Name => TaskAction.GreenscreenBackground;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.RobustVideoMatting);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png }, new[] { FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GreenscreenBackgroundInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output =>
        {
            if (input.Format == FileFormat.Jpeg && data.Color is null)
                return TaskRequirement.EnsureFormat(output, FileFormat.Png);
            return TaskRequirement.EnsureSameFormat(output, input);
        }));

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, GreenscreenBackgroundInfo data)
    {
        var input = files.InputFiles.MaxBy(f => f.Format).ThrowIfNull("Could not find input file");

        var outputformat = input.Format;
        if (input.Format == FileFormat.Jpeg && data.Color is null)
            outputformat = FileFormat.Png;

        // TODO: support cpu or not? not easy to do actually
        var pylaunch = "python"
            + $" -u" // unbuffered output, for progress tracking
            + $" inference.py"
            + $" --device cuda"
            + $" --input-source '{input.Path}'"
            + $" --output-composition '{files.OutputFiles.New().New(outputformat).Path}'"
            + $" --checkpoint 'models/mobilenetv3/rvm_mobilenetv3.pth'"
            + $" --variant mobilenetv3"
            + $" --output-type file"
            + $" --seq-chunk 1"; // parallel
        if (data.Color is not null)
            pylaunch += $" --background-color {data.Color.R} {data.Color.G} {data.Color.B}";

        await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(context, PluginType.RobustVideoMatting, pylaunch, onRead, context);


        void onRead(bool err, object obj)
        {
            // 1/1024
            var str = obj.ToString();
            if (str is null) return;

            var spt = str.Split('/');
            if (spt.Length != 2) return;

            if (!int.TryParse(spt[0], out var left))
                return;
            if (!int.TryParse(spt[1], out var right))
                return;

            context.SetProgress((float) left / right);
        }
    }
}