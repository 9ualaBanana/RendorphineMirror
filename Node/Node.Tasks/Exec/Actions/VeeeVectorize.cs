namespace Node.Tasks.Exec.Actions;

public class VeeeVectorize : PluginAction<VeeeVectorizeInfo>
{
    public override TaskAction Name => TaskAction.VeeeVectorize;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.VeeeVectorizer);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, VeeeVectorizeInfo data)
    {
        // 2x jpg and 2x eps per LOD
        var formats = Enumerable.Repeat(new[] { FileFormat.Jpeg, FileFormat.Jpeg, FileFormat.Eps, FileFormat.Eps }, data.Lod.Length).SelectMany(a => a).ToArray();

        return files.EnsureOutputFormats(formats);
    }

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, VeeeVectorizeInfo data)
    {
        var inputfile = files.InputFiles.Single().Path;
        inputfile = await ProcessLauncher.GetWinPath(inputfile);

        var outputdir = files.OutputFiles.Directory;

        var exepath = context.GetPlugin(PluginType.VeeeVectorizer).Path;

        var plugindir = Path.GetDirectoryName(exepath)!;
        var veeeoutdir = Path.Combine(plugindir, "out");

        // assuming we aren't executing several of veee.exe simultaneously
        if (Directory.Exists(veeeoutdir)) Directory.Delete(veeeoutdir, true);
        Directory.CreateDirectory(veeeoutdir);
        File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(inputfile, data.Lod, context));

        await new ProcessLauncher(exepath, inputfile) { WineSupport = true, Logging = { Logger = context }, Timeout = TimeSpan.FromMinutes(5) }
            .ExecuteAsync();

        Directory.Delete(outputdir, true);
        try { Directory.Move(veeeoutdir, outputdir); }
        catch (IOException)
        {
            // Directory.Move fails when trying to move cross-device
            Directories.Copy(veeeoutdir, outputdir);
            Directory.Delete(veeeoutdir, true);
        }

        files.OutputFiles.New().AddFromLocalPath(outputdir);
    }


    static string GetConfig(string imagefile, IReadOnlyCollection<int> lods, ILoggable? logger = null)
    {
        var image = Image<Rgba32>.Load(imagefile);

        // min size for vectorized jpegs is 16MP, and we add 1MP just in case
        const int minsize = (16 + 1) * 1024 * 1024;

        var mp = image.Width * image.Height;

        var outwidth = image.Width;
        if (minsize > mp)
        {
            var scale = minsize / (double) mp;
            outwidth = (int) (image.Width * Math.Sqrt(scale));
        }

        logger?.LogInfo($"Vectorizing image {imagefile} with outwidth {outwidth} and lods [{string.Join(", ", lods)}]");

        return $"""
            <CONFIG Version="1" ProfilesDir="Veee" Lang="Data\lang_en.xml" W="1920" H="1080" ShowFps="1" LastUser="no" outdir="out" outsize="{outwidth}">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
            </CONFIG>
            """;
    }
}