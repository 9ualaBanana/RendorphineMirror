namespace Node.Tasks.Exec.Actions;

public class VeeeVectorize : FilePluginActionInfo<VeeeVectorizeInfo>
{
    public override TaskAction Name => TaskAction.VeeeVectorize;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.VeeeVectorizer);
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, VeeeVectorizeInfo data)
    {
        // 2x jpg and 2x eps per LOD
        var formats = Enumerable.Repeat(new[] { FileFormat.Jpeg, FileFormat.Jpeg, FileFormat.Eps, FileFormat.Eps }, data.Lod.Length).SelectMany(a => a).ToArray();

        return files.EnsureOutputFormats(formats);
    }


    class Executor : ExecutorBase
    {
        public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, VeeeVectorizeInfo data)
        {
            var inputfile = input.Files.Single().Path;
            inputfile = await ProcessLauncher.GetWinPath(inputfile);

            var outputdir = input.ResultDirectory;
            var exepath = PluginList.GetPlugin(PluginType.VeeeVectorizer).Path;

            var plugindir = Path.GetDirectoryName(exepath)!;
            var veeeoutdir = Path.Combine(plugindir, "out");

            // assuming we aren't executing several of veee.exe simultaneously
            if (Directory.Exists(veeeoutdir)) Directory.Delete(veeeoutdir, true);
            Directory.CreateDirectory(veeeoutdir);
            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(inputfile, data.Lod));

            await new ProcessLauncher(exepath, inputfile) { WineSupport = true, Logging = { ILogger = Logger }, Timeout = TimeSpan.FromMinutes(5) }
                .ExecuteAsync();

            Directory.Delete(outputdir, true);
            try { Directory.Move(veeeoutdir, outputdir); }
            catch (IOException)
            {
                // Directory.Move fails when trying to move cross-device
                Directories.Copy(veeeoutdir, outputdir);
                Directory.Delete(veeeoutdir, true);
            }

            var output = new TaskFileOutput(outputdir);
            output.Files.New().AddFromLocalPath(outputdir);

            return output;
        }


        string GetConfig(string imagefile, IReadOnlyCollection<int> lods)
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

            Logger?.LogInformation($"Vectorizing image {imagefile} with outwidth {outwidth} and lods [{string.Join(", ", lods)}]");

            return $"""
                <CONFIG Version="1" ProfilesDir="Veee" Lang="Data\lang_en.xml" W="1920" H="1080" ShowFps="1" LastUser="no" outdir="out" outsize="{outwidth}">
                    {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
                """;
        }
    }
}