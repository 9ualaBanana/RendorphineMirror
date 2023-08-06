using System.Runtime.Serialization;
using Autofac;

namespace Node.Tasks.Exec;

public class TaskHandlerList
{
    public IEnumerable<ITaskInputHandlerInfo> InputHandlerList => InputHandlers.Values;
    public IEnumerable<ITaskOutputHandlerInfo> OutputHandlerList => OutputHandlers.Values;
    public IReadOnlyDictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlerList => WatchingHandlers;

    readonly Dictionary<TaskInputType, ITaskInputHandlerInfo> InputHandlers = new();
    readonly Dictionary<TaskOutputType, ITaskOutputHandlerInfo> OutputHandlers = new();
    readonly Dictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlers = new();

    public required IComponentContext ComponentContext { get; init; }

    public void AutoInitializeHandlers()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes())
            .Where(x => x.IsAssignableTo(typeof(ITaskHandler)))
            .Where(x => x.IsClass && !x.IsAbstract);

        foreach (var type in types)
            AddHandler(type);
    }
    public void AddHandler(Type type)
    {
        if (type.IsAssignableTo(typeof(ITaskInputHandler)))
        {
            var handler = (ITaskInputHandler) Activator.CreateInstance(type)!;
            InputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(ITaskOutputHandler)))
        {
            var handler = (ITaskOutputHandler) Activator.CreateInstance(type)!;
            OutputHandlers[handler.Type] = handler;
        }
        if (type.IsAssignableTo(typeof(IWatchingTaskInputHandler)))
        {
            // FormatterServices.GetSafeUninitializedObject is being used to create valid object for getting only the .Type property
            // since all IWatchingTaskInputHandler object implement Type property using `=>` and not `{ get; } =`

            WatchingHandlers[((IWatchingTaskInputHandler) FormatterServices.GetSafeUninitializedObject(type)).Type] =
                task =>
                {
                    var handler = (IWatchingTaskInputHandler) Activator.CreateInstance(type, new object?[] { task })!;
                    handler.GetType().GetProperty("TaskHandlerList").ThrowIfNull().SetValue(handler, ComponentContext.Resolve<WatchingTaskHandler>());

                    return handler;
                };
        }
    }

    public ITaskInputHandlerInfo GetInputHandler(TaskBase task) => GetHandler(task.Input.Type);
    public ITaskOutputHandlerInfo GetOutputHandler(TaskBase task) => GetHandler(task.Output.Type);
    public ITaskInputHandlerInfo GetHandler(TaskInputType type) => InputHandlers[type];
    public ITaskOutputHandlerInfo GetHandler(TaskOutputType type) => OutputHandlers[type];
}
