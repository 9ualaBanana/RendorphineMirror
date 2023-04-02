using System.Globalization;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Newtonsoft.Json;

namespace NodeUI.Pages.MainWindowTabs;

public class TasksTab2 : Panel
{
    public TasksTab2()
    {
        var tabs = new TabbedControl();
        tabs.Add("Local", new LocalTaskManager());
        tabs.Add("Watching", new WatchingTaskManager());
        tabs.Add("Remote", new RemoteTaskManager());

        Children.Add(tabs);
    }


    abstract class TaskManager<T> : Panel
    {
        protected NodeCommon.Apis Api => Apis.Default;

        public TaskManager()
        {
            var data = CreateDataGrid();
            Children.Add(WrapGrid(data));

            LoadSetItems(data).Consume();
        }

        protected DataGrid CreateDataGrid()
        {
            var data = new DataGrid() { AutoGenerateColumns = false };
            data.BeginningEdit += (obj, e) => e.Cancel = true;

            CreateColumns(data);
            return data;
        }
        protected virtual Control WrapGrid(DataGrid grid)
        {
            return new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto *"),
                Children =
                {
                    new MPButton()
                    {
                        Text = "Reload",
                        OnClick = () => { grid.Items = Array.Empty<T>(); LoadSetItems(grid).Consume(); },
                    }.WithRow(0),
                    grid.WithRow(1),
                },
            };
        }
        protected abstract void CreateColumns(DataGrid data);

        protected async Task LoadSetItems(DataGrid grid) => grid.Items = await Load();
        protected abstract Task<IReadOnlyCollection<T>> Load();
    }
    abstract class NormalTaskManager : TaskManager<TaskBase>
    {
        protected override void CreateColumns(DataGrid data)
        {
            data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(ReceivedTask.Id)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "State", Binding = new Binding(nameof(ReceivedTask.State)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "FirstAction", Binding = new Binding(nameof(ReceivedTask.FirstAction)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding("Input.Type") });
            data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding("Output.Type") });

            data.Columns.Add(new DataGridTextColumn() { Header = "Server Host", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Host)}") });
            data.Columns.Add(new DataGridTextColumn() { Header = "Server Userid", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Userid)}") });
            data.Columns.Add(new DataGridTextColumn() { Header = "Server Nickname", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Nickname)}") });

            data.Columns.Add(new DataGridButtonColumn<DbTaskFullState>()
            {
                Header = "Cancel task",
                Text = "Cancel task",
                CreationRequirements = task => task.State < TaskState.Finished,
                SelfAction = async (task, self) =>
                {
                    var change = await Api.ChangeStateAsync(task, TaskState.Canceled);
                    await self.FlashErrorIfErr(change);

                    if (change) await LoadSetItems(data);
                },
            });
        }
    }
    class LocalTaskManager : NormalTaskManager
    {
        protected override Task<IReadOnlyCollection<TaskBase>> Load() =>
            new IReadOnlyList<TaskBase>[] { NodeGlobalState.Instance.QueuedTasks, NodeGlobalState.Instance.PlacedTasks, NodeGlobalState.Instance.ExecutingTasks, }
                .SelectMany(x => x)
                .DistinctBy(x => x.Id)
                .ToArray()
                .AsTask<IReadOnlyCollection<TaskBase>>();
    }
    class RemoteTaskManager : NormalTaskManager
    {
        protected override async Task<IReadOnlyCollection<TaskBase>> Load() =>
            await Api.GetMyTasksAsync(Enum.GetValues<TaskState>()).ThrowIfError();
    }
    class WatchingTaskManager : TaskManager<WatchingTask>
    {
        protected override void CreateColumns(DataGrid data)
        {
            data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(WatchingTask.Id)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "Policy", Binding = new Binding(nameof(WatchingTask.Policy)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "Action", Binding = new Binding(nameof(WatchingTask.TaskAction)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding($"{nameof(WatchingTask.Source)}.Type") });
            data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding($"{nameof(WatchingTask.Output)}.Type") });

            data.Columns.Add(new DataGridTextColumn() { Header = "Paused", Binding = new Binding(nameof(WatchingTask.IsPaused)) });

            data.Columns.Add(new DataGridButtonColumn<WatchingTask>()
            {
                Header = "Delete",
                Text = "Delete",
                SelfAction = async (task, self) =>
                {
                    var result = await LocalApi.Default.Get("tasks/delwatching", "Deleting watching task", ("taskid", task.Id));
                    await self.FlashErrorIfErr(result);

                    if (result) await LoadSetItems(data);
                },
            });
            data.Columns.Add(new DataGridButtonColumn<WatchingTask>()
            {
                Header = "Pause/Unpause",
                Text = "Pause/Unpause",
                SelfAction = async (task, self) =>
                {
                    var result = await LocalApi.Default.Get<WatchingTask>("tasks/pausewatching", "Pausing watching task", ("taskid", task.Id));
                    await self.FlashErrorIfErr(result);

                    if (result) await LoadSetItems(data);
                },
            });
        }

        protected override Task<IReadOnlyCollection<WatchingTask>> Load() =>
            (NodeGlobalState.Instance.WatchingTasks as IReadOnlyCollection<WatchingTask>).AsTask();
    }


    class ObjectToJsonConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => JsonConvert.SerializeObject(value);
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => JsonConvert.DeserializeObject((string) value!, targetType);
    }
    class DataGridButtonColumn<T> : DataGridColumn
    {
        public string? Text;
        public Action<T>? Action;
        public Action<T, MPButton>? SelfAction;
        public Func<T, bool>? CreationRequirements;

        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not T item) return new Control();

            var btn = new MPButton()
            {
                Text = Text ?? string.Empty,
                OnClick = () => Action?.Invoke(item),
                OnClickSelf = self => SelfAction?.Invoke(item, self),
            };
            btn.Bind(MPButton.IsVisibleProperty, new Binding("") { Converter = new FuncValueConverter<T, bool>(t => t is null ? false : CreationRequirements?.Invoke(t) ?? true) });

            return btn;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
}