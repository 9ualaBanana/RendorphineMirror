namespace Node.UI.Pages.MainWindowTabs;

public class RFProductsTab : Panel
{
    public RFProductsTab()
    {
        var content = new ScrollViewer()
        {
            Content = new StackPanel()
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
        readonly BindableDictionary<string, UIRFProduct> Products;

        public RFProductListPanel()
        {
            Products = NodeGlobalState.Instance.RFProducts.GetBoundCopy();

            var stack = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 20,
            };
            var amounttb = new TextBlock();
            Children.Add(new ScrollViewer()
            {
                Content = new StackPanel()
                {
                    Children =
                    {
                        amounttb,
                        stack,
                    },
                }
            });

            var controls = new Dictionary<string, RFProductUi>();
            Products.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                var products = Products.Value;
                amounttb.Text = $"Products: {products.Count}";

                foreach (var (key, control) in controls)
                {
                    if (!products.ContainsKey(key))
                    {
                        controls.Remove(key);
                        stack.Children.Remove(control);
                    }
                }

                foreach (var (key, product) in products.ToArray())
                    if (!controls.ContainsKey(key))
                        stack.Children.Add((controls[key] = new RFProductUi(product)));
            }, DispatcherPriority.Background), true);
        }
    }
    class RFProductUi : Panel
    {
        readonly UIRFProduct Product;

        public RFProductUi(UIRFProduct product)
        {
            Product = product;

            var grid = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new TextBox() { Text = $"ID: {product.Id}", IsReadOnly = true },
                    new TextBox() { Text = $"Path: {product.Path}", IsReadOnly = true },
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new MPButton()
                            {
                                Text = "Open directory",
                                OnClick = () =>
                                {
                                    var target = product.Path;
                                    if (!Directory.Exists(target))
                                        target = Path.GetDirectoryName(target)!;

                                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
                                },
                            },
                            new MPButton()
                            {
                                Text = "Delete from DB (no files deleted)",
                                OnClickSelf = async (self) =>
                                {
                                    var result = await LocalApi.Default.Post("deleterfproduct", "Deleting an RF product", ("id", product.Id));
                                    await self.Flash(result);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Upload to TurboSquid",
                                OnClickSelf = async (self) =>
                                {
                                    var result = await LocalApi.Default.Post("upload3drfproduct", "Uploading 3d rfproduct to turbosquid",
                                        ("target", "turbosquid"), ("id", product.Id)
                                    );
                                    await self.Flash(result);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Upload to TurboSquid using account from _Submit.json",
                                OnClickSelf = async (self) =>
                                {
                                    var result = await LocalApi.Default.Post("upload3drfproductsubmitjson", "Uploading 3d rfproduct to turbosquid",
                                        ("target", "turbosquid"), ("id", product.Id)
                                    );
                                    await self.Flash(result);
                                },
                            },
                        },
                    },
                },
            };
            Children.Add(grid);
        }
    }
}

