using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public class VeeeVectorizeInfo
{
    [JsonProperty("lod")]
    [ArrayRanged(min: 1), Ranged(1, 10_000)]
    public int[] Lods = null!;
}


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
            task.InputFile.ThrowIfNull();
            var outputdir = task.FSOutputDirectory();

            var exepath = task.GetPlugin().GetInstance().Path;

            // quotes are important here, ddo not remove
            var args = "\"" + task.InputFile + "\"";

            var plugindir = Path.GetDirectoryName(exepath)!;
            var outdir = Path.Combine(plugindir, "out");

            // assuming we aren't executing several of veee.exe simultaneously
            if (Directory.Exists(outdir)) Directory.Delete(outdir, true);
            Directory.CreateDirectory(outdir);
            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(data.Lods));

            await ExecuteProcess(exepath, args, false, delegate { }, task);

            Directory.Delete(outputdir, true);
            Directory.Move(outdir, outputdir);
            foreach (var file in Directory.GetFiles(outputdir))
            {
                // bluenight_dark.250.g.eps > 250.g
                var postfix = "." + string.Join('.', file.Split('.')[^3..^1]);
                await UploadResult(task, output, file, postfix);
            }
        }


        static string GetConfig(IEnumerable<int> lods) =>
            $@"
                <CONFIG Version=""1"" ProfilesDir=""Veee"" Lang=""Data\lang_en.xml"" W=""1920"" H=""1080"" ShowFps=""1"" LastUser=""no"" outdir = ""out"">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
            ";
    }
}