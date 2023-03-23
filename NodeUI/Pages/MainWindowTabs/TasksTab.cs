using Newtonsoft.Json;

namespace NodeUI.Pages.MainWindowTabs;

public class TasksTab : Panel
{
    public TasksTab()
    {
        var allplaced = null as StackPanel;
        allplaced = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new MPButton()
                {
                    Text = "Fetch all active placed tasks",
                    OnClickSelf = async self =>
                    {
                        allplaced.ThrowIfNull();

                        var tasks = await Apis.Default.GetMyTasksAsync(new[] { TaskState.Queued, TaskState.Input, TaskState.Active, TaskState.Output, TaskState.Validation, });
                        await self.FlashErrorIfErr(tasks);
                        if (!tasks) return;

                        if (allplaced.Children.Count > 1)
                            allplaced.Children.RemoveAt(1);
                        allplaced.Children.Add(NamedList.CreateRaw("ALL active placed tasks", tasks.ThrowIfError(), placedTasksCreate));
                    },
                },
            },
        };


        var scroll = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 20,
                Children =
                {
                    NamedList.Create("Executing tasks", NodeGlobalState.Instance.ExecutingTasks, execTasksCreate),
                    NamedList.Create("Watching tasks", NodeGlobalState.Instance.WatchingTasks, watchingTasksCreate),
                    NamedList.Create("Placed tasks", NodeGlobalState.Instance.PlacedTasks, placedTasksCreate),
                    allplaced,
                },
            },
        };

        Children.Add(scroll);


        IControl execTasksCreate(ReceivedTask task)
        {
            var statustb = new TextBlock();

            return new Expander()
            {
                Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                Content = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                        new TextBlock() { Text = $"Input: {JsonConvert.SerializeObject( task.Info.Input,Formatting.None)}" },
                        new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject( task.Info.Output,Formatting.None)}" },
                        statustb,
                    },
                },
            };
        }
        IControl placedTasksCreate(DbTaskFullState task)
        {
            var statustb = new TextBlock();
            var statusbtn = new MPButton()
            {
                Text = "Update status",
                OnClickSelf = async self => await updateState(self),
            };
            var cancelbtn = new MPButton()
            {
                Text = "Cancel task",
                OnClickSelf = async self =>
                {
                    var cstate = await Apis.Default.ChangeStateAsync(task, TaskState.Canceled);
                    await self.FlashErrorIfErr(cstate);
                    if (!cstate) return;

                    await updateState(self);
                },
            };

            async Task updateState(MPButton button)
            {
                var state = await Apis.Default.GetTaskStateAsyncOrThrow(task);
                await button.FlashErrorIfErr(state);
                if (!state) return;

                statustb.Text = JsonConvert.SerializeObject(state.Value, Formatting.None);
            }

            return new Expander()
            {
                Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                Content = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                        new TextBlock() { Text = $"Input: {JsonConvert.SerializeObject(task.Info.Input, Formatting.None)}" },
                        new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject(task.Info.Output, Formatting.None)}" },
                        statustb,
                        new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                statusbtn,
                                cancelbtn,
                            },
                        },
                    },
                },
            };
        }
        IControl watchingTasksCreate(WatchingTask task)
        {
            return new Expander()
            {
                Header = $"{task.Id} {NodeGlobalState.Instance.GetPluginTypeFromAction(task.TaskAction)} {task.TaskAction}",
                Content = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock() { Text = $"Data: {task.TaskData.ToString(Formatting.None)}" },
                        new TextBlock() { Text = $"Source: {JsonConvert.SerializeObject(task.Source, Formatting.None)}" },
                        new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject(task.Output, Formatting.None)}" },
                    },
                },
            };
        }
    }
}