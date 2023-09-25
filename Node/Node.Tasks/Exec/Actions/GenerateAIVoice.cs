namespace Node.Tasks.Exec.Actions;

public class GenerateAIVoice : FilePluginActionInfo<GenerateAIVoiceInfo>
{
    public override TaskAction Name => TaskAction.GenerateAIVoice;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { Array.Empty<FileFormat>() };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateAIVoiceInfo data) =>
        files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureFormat(output, FileFormat.Png));

    class Executor : ExecutorBase
    {
        public required DataDirs Dirs { get; init; }
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }

        public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, GenerateAIVoiceInfo data)
        {
            var result = new TaskFileOutput(input.ResultDirectory);
            var resultfile = result.Files.New().New(FileFormat.Mp3).Path;

            var parameters = Api.AddSessionId(("taskid", ApiTask.Id), ("text", data.Text), ("voiceid", data.VoiceId.ThrowIfNull()), ("modelid", data.ModelId.ThrowIfNull()));
            await Api.Api.ApiPostFile("https://t.microstock.plus:7899/eleven/tts", resultfile, "Generating AI voice using ElevenLabs", parameters)
                .ThrowIfError();

            return result;
        }
    }
}
