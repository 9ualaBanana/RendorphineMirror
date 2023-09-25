namespace Node.Tasks.Models.ExecInfo;

[JsonConverter(typeof(StringEnumConverter))]
public enum GenerateAIVoiceSource
{
    ElevenLabs,
}

