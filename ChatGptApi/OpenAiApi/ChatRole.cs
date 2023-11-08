using System.Runtime.Serialization;

namespace ChatGptApi.OpenAiApi;

[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRole
{
    [EnumMember(Value = "user")] User,
    [EnumMember(Value = "assistant")] Assistant,
    [EnumMember(Value = "system")] System,
}
