using System.Text.Json;

namespace Node.Tasks.Models;

public record TaskInfo(
    string UserId,
    long Registered,
    TaskObject Object,
    JsonElement Input,
    JsonElement Output,
    JsonElement State,
    int Progress,
    string Origin,
    ServerInfo? Server)
{
    // Define explicit constructor.
    // Make properties out of Input, Output, State that are backed by
    // backing fields that are assigned the objects of proper types
    // (i.e. TMTaskInputInfoMPlus, TMTaskOutputInfoMPlus, etc.)
    // to which initially deserialized JsonElements are further deserialized.
}
