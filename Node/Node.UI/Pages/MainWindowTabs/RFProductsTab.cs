namespace Node.UI.Pages.MainWindowTabs;

public class RFProductsTab : Panel
{
    public RFProductsTab()
    {
        var stack = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new CreateRFProductPanel()
                    .Named("Create"),
            },
        };

        this.Children.Add(stack);
    }


    class CreateRFProductPanelData
    {
        [LocalFile] public required string Idea { get; init; }

        [LocalDirectory] public required string DirectoryContainer { get; init; }
        [LocalFile] public required string ArchiveContainer { get; init; }
    }
    class CreateRFProductPanel : Panel
    {
        public CreateRFProductPanel()
        {
            var idea = new TextBox();
            var container = new TextBox();

            var ideapanel = new Grid()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("* Auto"),
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
            return result.FirstOrDefault()?.Path.AbsolutePath ?? string.Empty;
        }
        async Task<string> OpenDirectoryPicker()
        {
            var result = await ((Window) VisualRoot!).StorageProvider.OpenFolderPickerAsync(new() { AllowMultiple = false });
            return result.FirstOrDefault()?.Path.AbsolutePath ?? string.Empty;
        }
    }
}
