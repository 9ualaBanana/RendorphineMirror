using Autofac;
using System.Runtime.Serialization;

namespace Node.Tasks.Exec;

public class TaskHandlerList
{
    public IEnumerable<ITaskInputHandler> InputHandlerList => InputHandlers.Values;
    public IEnumerable<ITaskOutputHandler> OutputHandlerList => OutputHandlers.Values;
    public IReadOnlyDictionary<WatchingTaskInputType, Func<WatchingTask, IWatchingTaskInputHandler>> WatchingHandlerList => WatchingHandlers;

    readonly Dictionary<TaskInputType, ITaskInputHandler> InputHandlers = new();
    readonly Dictionary<TaskOutputType, ITaskOutputHandler> OutputHandlers = new();
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

    public ITaskInputHandler GetInputHandler(TaskBase task) => GetHandler(task.Input.Type);
    public ITaskOutputHandler GetOutputHandler(TaskBase task) => GetHandler(task.Output.Type);
    public ITaskInputHandler GetHandler(TaskInputType type) => InputHandlers[type];
    public ITaskOutputHandler GetHandler(TaskOutputType type) => OutputHandlers[type];
}
