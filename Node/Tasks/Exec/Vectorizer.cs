using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public record VeeeVectorizeInfo(
    [property: JsonProperty("lod")] ImmutableArray<int> Lods
);

public static class VectorizerTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new VeeeVectorize() };


    class VeeeVectorize : InputOutputPluginAction<VeeeVectorizeInfo>
    {
        public override string Name => "VeeeVectorize";
        public override PluginType Type => PluginType.VeeeVectorizer;
        public override FileFormat FileFormat => FileFormat.Jpeg;

        protected override async Task Execute(ReceivedTask task, VeeeVectorizeInfo data, ITaskInput input, ITaskOutput output)
        {
            var outputfile = task.FSOutputFile();

            var exepath = task.GetPlugin().GetInstance().Path;
            var args = task.InputFile;

            var plugindir = Path.GetDirectoryName(exepath)!;
            var outdir = Path.Combine(plugindir, "out");

            // assuming we aren't executing several of veee.exe simultaneously
            if (Directory.Exists(outdir)) Directory.Delete(outdir, true);
            Directory.CreateDirectory(outdir);
            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(data.Lods));

            await ExecuteProcess(exepath, args, false, delegate { }, task);
            await UploadResult(task, output, outputfile);
        }


        static string GetConfig(IEnumerable<int> lods) =>
            $@"
                <CONFIG Version=""1"" ProfilesDir=""Veee"" Lang=""Data\lang_en.xml"" W=""1920"" H=""1080"" ShowFps=""1"" LastUser=""no"" outdir = ""out"">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
            ";
    }
}