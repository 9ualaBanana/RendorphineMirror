using static Node.Tests.GenericTasksTests;
using static Node.Tests.TaskTesting;

namespace Node.Tests;

public class LocalTests
{
    public required DataDirs Dirs { get; init; }
    public required ILifetimeScope Context { get; init; }
    public required ILogger<LocalTests> Logger { get; init; }

    public async Task Run()
    {
        Logger.LogInformation("Running tests...");

        await ElevenLabsTest();
    }

    async Task ElevenLabsTest()
    {
        using var ctx = Context.BeginLifetimeScope(builder =>
        {
            builder.RegisterType<HttpClient>()
                .SingleInstance();
            builder.RegisterType<ElevenLabsApi>()
                .SingleInstance();
            builder.RegisterType<ElevenLabsApis>()
                .WithParameter("apiKey", File.ReadAllText("elevenlabsapikey").Trim())
                .SingleInstance();
        });

        var api = ctx.Resolve<ElevenLabsApis>();


        var voices = await api.GetVoiceListAsync()
            .ThrowIfError();

        var ttsfile = Dirs.NamedTempFile("eleven.mp3");
        await api.TextToSpeechAsync(voices[0].VoiceId, "eleven_monolingual_v1", "Hello I am the bot from the api test, wow!", ttsfile)
            .ThrowIfError();
    }
}
