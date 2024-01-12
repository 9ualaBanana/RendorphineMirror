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
                new RFProductListPanel()
                    .Named("List"),
            },
        };

        this.Children.Add(stack);
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
    class RFProductListPanel : Panel
    {
        readonly IReadOnlyBindableCollection<RFProduct> Products;

        public RFProductListPanel()
        {
            Products = NodeGlobalState.Instance.RFProducts.GetBoundCopy();

            var stack = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 20,
            };
            Children.Add(new ScrollViewer() { Content = stack });

            Products.SubscribeChanged(() =>
            {
                stack.Children.Clear();

                foreach (var product in Products)
                    stack.Children.Add(new RFProductUi(product));
            }, true);
        }
    }
    class RFProductUi : Panel
    {
        readonly RFProduct Product;

        public RFProductUi(RFProduct product)
        {
            Product = product;

            var grid = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto Auto Auto"),
                ColumnDefinitions = ColumnDefinitions.Parse("Auto *"),
                Children =
                {
                    new TextBlock() { Text = $"ID: {product.ID}" },
                    new TextBlock() { Text = $"Path: {product.Path}" }.WithRow(1),
                    new MPButton()
                    {
                        Text = "Open directory",
                        OnClick = () =>
                        {
                            var target = Product.Path;
                            if (!Directory.Exists(target))
                                target = Path.GetDirectoryName(target)!;

                            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
                        },
                    }.WithRow(2),
                    new MPButton()
                    {
                        Text = "Delete from DB (no files deleted)",
                        OnClickSelf = async (self) =>
                        {
                            var result = await LocalApi.Default.Post("deleterfproduct", "Deleting an RF product", ("id", product.ID));
                            await self.Flash(result);
                        },
                    }.WithRow(3),
                },
            };
            Children.Add(grid);
        }
    }
}
