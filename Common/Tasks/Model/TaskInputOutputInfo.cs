using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo
{
    TaskInputOutputType Type { get; }

    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskInputInfo : ITaskInputOutputInfo { }
public interface ITaskOutputInfo : ITaskInputOutputInfo { }


public static class TaskInputOutputInfo
{
    public static ImmutableDictionary<TaskInputOutputType, Type> Inputs;
    public static ImmutableDictionary<TaskInputOutputType, Type> Outputs;

    static TaskInputOutputInfo()
    {
        Inputs = new ITaskInputInfo[]
        {
            createObject<MPlusTaskInputInfo>(),
            createObject<DownloadLinkTaskInputInfo>(),
            createObject<TorrentTaskInputInfo>(),
            createObject<UserTaskInputInfo>(),
        }.ToImmutableDictionary(x => x.Type, x => x.GetType());

        Outputs = new ITaskOutputInfo[]
        {
            createObject<MPlusTaskOutputInfo>(),
            createObject<TorrentTaskOutputInfo>(),
            createObject<UserTaskOutputInfo>(),
        }.ToImmutableDictionary(x => x.Type, x => x.GetType());


        // this should return valid object for getting only the .Type property
        // since all TaskInOutputInfo object implement ITaskInOutputInfo.Type property using `=>` and not `{ get; } =`
        static T createObject<T>() where T : ITaskInputOutputInfo => (T) FormatterServices.GetSafeUninitializedObject(typeof(T));
    }


    public static ITaskInputInfo DeserializeInput(JObject input) => (ITaskInputInfo) input.ToObject(TaskInputOutputInfo.Inputs[input.GetValue("type", StringComparison.OrdinalIgnoreCase)!.ToObject<TaskInputOutputType>()])!;
    public static ITaskOutputInfo DeserializeOutput(JObject output) => (ITaskOutputInfo) output.ToObject(TaskInputOutputInfo.Outputs[output.GetValue("type", StringComparison.OrdinalIgnoreCase)!.ToObject<TaskInputOutputType>()])!;
}