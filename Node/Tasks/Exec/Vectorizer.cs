using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Node.Tasks.Exec;

public static class VectorizerTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new VeeeVectorize() };


    class VeeeVectorize : InputOutputPluginAction<VeeeVectorizeInfo>
    {
        public override TaskAction Name => TaskAction.VeeeVectorize;
        public override PluginType Type => PluginType.VeeeVectorizer;

        public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
            new[] { new[] { FileFormat.Jpeg },  new[] { FileFormat.Png } };

        protected override OperationResult ValidateOutputFiles(IOTaskCheckData files, VeeeVectorizeInfo data)
        {
            // 2x jpg and 2x eps per LOD
            var amount = data.Lods.Length * 2;
            var formats = Enumerable.Repeat(new[] { FileFormat.Jpeg, FileFormat.Jpeg, FileFormat.Eps, FileFormat.Eps }, data.Lods.Length).SelectMany(a => a).ToArray();

            return files.EnsureOutputFormats(formats);
        }

        protected override async Task ExecuteImpl(ReceivedTask task, IOTaskExecutionData files, VeeeVectorizeInfo data)
        {
            var inputfile = files.InputFiles.Single().Path;
            var outputdir = task.FSOutputDirectory();

            var exepath = PluginPath;

            // quotes are important here, ddo not remove
            var args = "\"" + await GetWinPath(inputfile) + "\"";

            var plugindir = Path.GetDirectoryName(exepath)!;
            var veeeoutdir = Path.Combine(plugindir, "out");

            // assuming we aren't executing several of veee.exe simultaneously
            if (Directory.Exists(veeeoutdir)) Directory.Delete(veeeoutdir, true);
            Directory.CreateDirectory(veeeoutdir);
            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(inputfile, data.Lods, task));

            await ExecuteProcessWithWineSupport(exepath, args, delegate { }, task);

            Directory.Delete(outputdir, true);
            Directory.Move(veeeoutdir, outputdir);

            files.OutputFiles.AddFromLocalPath(outputdir);
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

            return $@"
                <CONFIG Version=""1"" ProfilesDir=""Veee"" Lang=""Data\lang_en.xml"" W=""1920"" H=""1080"" ShowFps=""1"" LastUser=""no"" outdir=""out"" outsize=""{outwidth}"">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
            ".TrimLines();
        }
    }
}