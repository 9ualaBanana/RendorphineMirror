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
        public override TaskFileFormatRequirements InputRequirements { get; } = new TaskFileFormatRequirements(FileFormat.Jpeg);
        public override TaskFileFormatRequirements OutputRequirements { get; } = new TaskFileFormatRequirements()
            .RequiredAtLeast(FileFormat.Jpeg, 1)
            .RequiredAtLeast(FileFormat.Eps, 1);

        protected override async Task ExecuteImpl(ReceivedTask task, VeeeVectorizeInfo data)
        {
            var inputfile = task.FSInputFile();
            var outputdir = task.FSOutputDirectory();

            var exepath = task.GetPlugin().GetInstance().Path;

            // quotes are important here, ddo not remove
            var args = "\"" + inputfile + "\"";

            var plugindir = Path.GetDirectoryName(exepath)!;
            var veeeoutdir = Path.Combine(plugindir, "out");

            // assuming we aren't executing several of veee.exe simultaneously
            if (Directory.Exists(veeeoutdir)) Directory.Delete(veeeoutdir, true);
            Directory.CreateDirectory(veeeoutdir);
            File.WriteAllText(Path.Combine(plugindir, "config.xml"), GetConfig(data.Lods));

            await ExecuteProcess(exepath, args, delegate { }, task);

            Directory.Delete(outputdir, true);
            Directory.Move(veeeoutdir, outputdir);

            task.AddOutputFromLocalPath(outputdir);
        }


        static string GetConfig(IEnumerable<int> lods) =>
            $@"
                <CONFIG Version=""1"" ProfilesDir=""Veee"" Lang=""Data\lang_en.xml"" W=""1920"" H=""1080"" ShowFps=""1"" LastUser=""no"" outdir = ""out"">
                {string.Join(Environment.NewLine, lods.Select(lod => @$"<EXPORT mode=""0"" lod=""{lod}""/>"))}
                </CONFIG>
            ";
    }
}