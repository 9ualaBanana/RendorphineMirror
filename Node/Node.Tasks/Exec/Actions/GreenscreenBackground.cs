namespace Node.Tasks.Exec.Actions;

public class GreenscreenBackground : FilePluginAction<GreenscreenBackgroundInfo>
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

    public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, GreenscreenBackgroundInfo data)
    {
        var file = input.Files.MaxBy(f => f.Format).ThrowIfNull("Could not find input file");

        var outputformat = file.Format;
        if (file.Format == FileFormat.Jpeg && data.Color is null)
            outputformat = FileFormat.Png;

        var output = new TaskFileOutput(input.ResultDirectory);

        // TODO: support cpu or not? not easy to do actually
        var pylaunch = "python"
            + $" -u" // unbuffered output, for progress tracking
            + $" inference.py"
            + $" --device cuda"
            + $" --input-source '{file.Path}'"
            + $" --output-composition '{output.Files.New().New(outputformat).Path}'"
            + $" --checkpoint 'models/mobilenetv3/rvm_mobilenetv3.pth'"
            + $" --variant mobilenetv3"
            + $" --output-type file"
            + $" --seq-chunk 1"; // parallel
        if (data.Color is not null)
            pylaunch += $" --background-color {data.Color.R} {data.Color.G} {data.Color.B}";

        await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(PluginList, PluginType.RobustVideoMatting, pylaunch, onRead, Logger);
        return output;


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

            ProgressSetter.Set((float) left / right);
        }
    }
}