using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputOutputType
{
    User,
    MPlus,
}
