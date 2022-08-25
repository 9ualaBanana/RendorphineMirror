using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public record VeeeVectorizeInfo(
    [property: JsonProperty("lod")] ImmutableArray<int> Lods
);

public static class VectorizerTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new VeeeVectorize() };


    class VeeeVectorize : PluginAction<VeeeVectorizeInfo>
    {
        public override string Name => "VeeeVectorize";
        public override PluginType Type => PluginType.VeeeVectorizer;
        public override FileFormat FileFormat => FileFormat.Jpeg;

        protected override async Task<string> Execute(ReceivedTask task, VeeeVectorizeInfo data)
        {
            task.InputFile.ThrowIfNull();
            var output = task.FSOutputFile();

            var exepath = task.GetPlugin().GetInstance().Path;
            var args = task.InputFile;

            var plugindir = Path.GetDirectoryName(exepath)!;
            var outdir = Path.Combine(plugindir, "out");
            if (Directory.Exists(outdir))
                Directory.Delete(outdir, true);
            Directory.CreateDirectory(outdir);

            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(data.Lods));

            await ExecuteProcess(exepath, args, false, delegate { }, task);
            return output;
        }


        static string GetConfig(IEnumerable<int> lods) =>
            $@"
                <CONFIG Version=""1"" ProfilesDir=""Veee"" Lang=""Data\lang_en.xml"" W=""1920"" H=""1080"" ShowFps=""1"" LastUser=""no"" outdir = ""out"">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
            ";
    }
}