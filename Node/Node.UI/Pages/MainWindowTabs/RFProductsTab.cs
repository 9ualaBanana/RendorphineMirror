using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

namespace Node.UI.Pages.MainWindowTabs;

public class RFProductsTab : Panel
{
    public RFProductsTab()
    {
        var content = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new CreateRFProductPanel()
                    .Named("Create"),
                new RFProductListPanel()
                    .Named("List"),
                new TextBlock()
                    .Named("~ The end ~"),
            },
        };

        Children.Add(content);
    }


    class CreateRFProductPanel : Panel
    {
        public CreateRFProductPanel()
        {
            var idea = new TextBox();
            var container = new TextBox();

            var ideapanel = new Grid()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("* Auto Auto"),
                Children =
                {
                    idea.WithColumn(0),
                    new MPButton()
                    {
                        Text = "Choose idea file",
                        OnClick = async () =>
                        {
                            var result = await OpenFilePicker();
                            idea.Text = result;
                        },
                    }.WithColumn(1),
                    new MPButton()
                    {
                        Text = "Choose idea directory",
                        OnClick = async () =>
                        {
                            var result = await OpenDirectoryPicker();
                            idea.Text = result;
                        },
                    }.WithColumn(2),
                },
            };
            var containerpanel = new Grid()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("* Auto Auto"),
                Children =
                {
                    container.WithColumn(0),
                    new MPButton()
                    {
                        Text = "Choose container archive",
                        OnClick = async () =>
                        {
                            var result = await OpenFilePicker();
                            container.Text = result;
                        },
                    }.WithColumn(1),
                    new MPButton()
                    {
                        Text = "Choose container directory",
                        OnClick = async () =>
                        {
                            var result = await OpenDirectoryPicker();
                            container.Text = result;
                        },
                    }.WithColumn(2),
                },
            };

            var stack = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    ideapanel,
                    containerpanel,
                    new MPButton()
                    {
                        Text = "Create",
                        OnClickSelf = async (self) =>
                        {
                            using var _lock = self.Lock();

                            if (string.IsNullOrWhiteSpace(idea.Text))
                            {
                                await self.FlashError("No idea");
                                return;
                            }
                            if (string.IsNullOrWhiteSpace(container.Text))
                            {
                                await self.FlashError("No container");
                                return;
                            }
                            if (!File.Exists(idea.Text) && !Directory.Exists(idea.Text))
                            {
                                await self.FlashError("Idea file/dir doesn't exists");
                                return;
                            }

                            var result = await LocalApi.Default.Post("createrfproduct", "Creating an RF product", ("idea", idea.Text.Trim()), ("container", container.Text.Trim()));
                            await self.Flash(result);
                        },
                    },
                },
            };

            this.Children.Add(stack);
        }


        async Task<string> OpenFilePicker()
        {
            var result = await ((Window) VisualRoot!).StorageProvider.OpenFilePickerAsync(new() { AllowMultiple = false });
            return result.FirstOrDefault()?.Path.LocalPath ?? string.Empty;
        }
        async Task<string> OpenDirectoryPicker()
        {
            var result = await ((Window) VisualRoot!).StorageProvider.OpenFolderPickerAsync(new() { AllowMultiple = false });
            return result.FirstOrDefault()?.Path.LocalPath ?? string.Empty;
        }
    }
    class RFProductListPanel : Panel
    {
        readonly IReadOnlyBindableCollection<KeyValuePair<string, UIRFProduct>> Products;

        public RFProductListPanel()
        {
            Products = NodeGlobalState.Instance.RFProducts.GetBoundCopy();

            var dg = CreateDataGrid();
            var amounttb = new TextBlock();
            Children.Add(new StackPanel()
            {
                Children =
                {
                    amounttb,
                    dg,
                },
            });


            Products.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                amounttb.Text = $"Products: {Products.Count}";
                dg.ItemsSource = Products.Select(k => k.Value).ToArray();
            }, DispatcherPriority.Background), true);
        }

        DataGrid CreateDataGrid()
        {
            var data = new DataGrid() { AutoGenerateColumns = false };
            data.BeginningEdit += (obj, e) => e.Cancel = true;

            CreateColumns(data);
            return data;
        }
        void CreateColumns(DataGrid data)
        {
            data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(UIRFProduct.Id)) });
            data.Columns.Add(new DataGridTextColumn() { Header = "Path", Binding = new Binding(nameof(UIRFProduct.Path)) });

            data.Columns.Add(new DataGridButtonColumn<UIRFProduct>()
            {
                Header = "Open directory",
                Text = "Open directory",
                SelfAction = async (product, self) =>
                {
                    var target = product.Path;
                    if (!Directory.Exists(target))
                        target = Path.GetDirectoryName(target)!;

                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
                },
            });
            data.Columns.Add(new DataGridButtonColumn<UIRFProduct>()
            {
                Header = "Delete from DB only",
                Text = "Delete from DB only",
                SelfAction = async (product, self) =>
                {
                    var result = await LocalApi.Default.Post("deleterfproduct", "Deleting an RF product", ("id", product.Id));
                    await self.Flash(result);
                },
            });
        }


        class DataGridButtonColumn<T> : DataGridColumn
        {
            public string? Text;
            public Action<T>? Action;
            public Action<T, MPButton>? SelfAction;
            public Func<T, bool>? CreationRequirements;

            protected override Control GenerateElement(DataGridCell cell, object dataItem)
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

            protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
            protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
        }
    }
}
