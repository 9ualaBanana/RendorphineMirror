using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Model;

public interface ITaskInputOutputInfo { }
public interface ITaskInputInfo : ITaskInputOutputInfo
{
    TaskInputType Type { get; }

    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
public interface ITaskOutputInfo : ITaskInputOutputInfo
{
    TaskOutputType Type { get; }

    ValueTask InitializeAsync() => ValueTask.CompletedTask;
}


public static class TaskInputOutputInfo
{
    public static ImmutableDictionary<TaskInputType, ITaskInputInfo> Inputs;
    public static ImmutableDictionary<TaskOutputType, ITaskOutputInfo> Outputs;

    static TaskInputOutputInfo()
    {
        Inputs = new ITaskInputInfo[]
        {
            createObject<MPlusTaskInputInfo>(),
            createObject<DownloadLinkTaskInputInfo>(),
            createObject<TorrentTaskInputInfo>(),
        }.ToImmutableDictionary(x => x.Type);

        Outputs = new ITaskOutputInfo[]
        {
            createObject<MPlusTaskOutputInfo>(),
            createObject<TorrentTaskOutputInfo>(),
        }.ToImmutableDictionary(x => x.Type);


        // this should return valid object for getting only the .Type property
        // since all TaskInOutputInfo object implement ITaskInOutputInfo.Type property using `=>` and not `{ get; } =`
        static T createObject<T>() where T : ITaskInputOutputInfo => (T) FormatterServices.GetSafeUninitializedObject(typeof(T));
    }


    public static ITaskInputInfo DeserializeInput(JObject input) => TaskInputOutputInfo.Inputs[input["type"]!.Value<TaskInputType>()];
    public static ITaskOutputInfo DeserializeOutput(JObject output) => TaskInputOutputInfo.Outputs[output["type"]!.Value<TaskOutputType>()];
}