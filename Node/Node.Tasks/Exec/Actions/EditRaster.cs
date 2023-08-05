namespace Node.Tasks.Exec.Actions;

public class EditRaster : FFMpegMediaEditAction<EditRasterInfo>
{
    public override TaskAction Name => TaskAction.EditRaster;

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, EditRasterInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

    protected override void AddFilters(EditRasterInfo data, TaskFileListList output, FileWithFormat input, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
    {
        base.AddFilters(data, output, input, ffprobe, launcher);

        launcher.Outputs.Add(new FFmpegLauncherOutput()
        {
            Codec = FFmpegLauncher.CodecFromStream(ffprobe.VideoStream),
            Output = output.New().New(input.Format).Path,
        });
    }
}
