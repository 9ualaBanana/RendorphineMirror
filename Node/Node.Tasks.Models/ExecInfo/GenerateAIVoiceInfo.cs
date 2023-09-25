namespace Node.Tasks.Models.ExecInfo;

public class GenerateAIVoiceInfo
{
    [Default(GenerateAIVoiceSource.ElevenLabs)]
    public GenerateAIVoiceSource Source { get; }

    public string Text { get; }

    public string? VoiceId { get; init; }
    public string? ModelId { get; init; }

    public GenerateAIVoiceInfo(GenerateAIVoiceSource source, string text)
    {
        Source = source;
        Text = text;
    }
}
