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
}