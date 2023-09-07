using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using NodeToUI.Requests;

namespace Node.UI.Pages;

public class TurboSquidModelInfoInputWindow : GuiRequestWindow
{
    readonly Bindable<EditableModelInfo?> NativeItem = new();

    public TurboSquidModelInfoInputWindow(InputTurboSquidModelInfoRequest request, Func<JToken, Task> onClick)
    {
        Width = 600;
        Height = 400;
        this.Bind(TitleProperty, "Input model info:");

        var infos = request.Infos.Select(r => new EditableModelInfo(NativeItem, r, request.FormatRenderers)).ToArray();

        var grid = new DataGrid() { AutoGenerateColumns = false, Items = infos, };
        grid.BeginningEdit += (obj, e) => e.Cancel = true;

        grid.Columns.Add(new DataGridCheckboxColumn() { Header = "Is native", });
        grid.Columns.Add(new DataGridFormatDropdownColumn() { Header = "Format", });
        grid.Columns.Add(new DataGridRendererDropdownColumn() { Header = "Renderer", });
        grid.Columns.Add(new DataGridFormatVersionColumn() { Header = "Format version", });
        grid.Columns.Add(new DataGridRendererVersionColumn() { Header = "Renderer version", });

        Content = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* Auto"),
            Children =
            {
                grid.WithRow(0),
                new MPButton()
                {
                    Text = "OK",
                    OnClickSelf = async self =>
                    {
                        foreach(var item in infos)
                        {
                            if (NativeItem.Value is null)
                            {
                                await self.FlashError("No native selected");
                                return;
                            }
                            if (item.Format.Value is null)
                            {
                                await self.FlashError("No format");
                                return;
                            }
                            if (item.Renderer.Value is null)
                            {
                                await self.FlashError("No renderer");
                                return;
                            }
                            if (item.FormatVersion.Value is null)
                            {
                                await self.FlashError("No format version");
                                return;
                            }
                            if (infos.Any(info => info.FormatVersion.Value is not null &&  info.FormatVersion.Value.Length != 0 && !double.TryParse(info.FormatVersion.Value, out _)))
                            {
                                await self.FlashError("Invalid format version");
                                return;
                            }
                            if (infos.Any(info => info.RendererVersion.Value is not null &&  info.RendererVersion.Value.Length != 0 && !double.TryParse(info.RendererVersion.Value, out _)))
                            {
                                await self.FlashError("Invalid renderer version");
                                return;
                            }

                            await onClick(JObject.FromObject(
                                new InputTurboSquidModelInfoRequest.Response(
                                    infos.Select(info => new InputTurboSquidModelInfoRequest.Response.ResponseModelInfo(
                                        info.IsNative.Value, info.Format.Value, info.Renderer.Value,
                                        double.Parse(info.FormatVersion.Value.ThrowIfNull(), CultureInfo.InvariantCulture), double.Parse(info.RendererVersion.Value.ThrowIfNull(), CultureInfo.InvariantCulture)
                                    ))
                                    .ToImmutableArray()
                                )
                            ));
                            Dispatcher.UIThread.Post(ForceClose);
                        }
                    },
                }.WithRow(1),
            },
        };
    }


    class EditableModelInfo
    {
        public ImmutableDictionary<string, ImmutableArray<string>> FormatRenderers;

        public Bindable<bool> IsNative { get; }
        public Bindable<string> Format { get; }
        public Bindable<string> Renderer { get; }
        public Bindable<string> FormatVersion { get; }
        public Bindable<string?> RendererVersion { get; }

        public EditableModelInfo(Bindable<EditableModelInfo?> nativeItem, InputTurboSquidModelInfoRequest.ModelInfo info, ImmutableDictionary<string, ImmutableArray<string>> formatRenderers)
        {
            FormatRenderers = formatRenderers;
            FormatVersion = new();
            RendererVersion = new();
            Format = new(formatRenderers.Keys.First());
            Renderer = new(formatRenderers[formatRenderers.Keys.First()][0]);

            IsNative = new(false);
            nativeItem.SubscribeChanged(() => IsNative.Value = object.ReferenceEquals(nativeItem.Value, this), true);
            IsNative.SubscribeChanged(() =>
            {
                if (!IsNative.Value && object.ReferenceEquals(nativeItem.Value, this))
                    nativeItem.Value = null;

                if (IsNative.Value)
                    nativeItem.Value = this;
            });
        }
    }


    class DataGridCheckboxColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var checkbox = new CheckBox();
            item.IsNative.SubscribeChanged(() => checkbox.IsChecked = item.IsNative.Value, true);
            checkbox.Subscribe(CheckBox.IsCheckedProperty, c => item.IsNative.Value = c == true);

            return checkbox;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
    class DataGridFormatDropdownColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var dropdown = new ComboBox()
            {
                Items = item.FormatRenderers.Keys,
                SelectedItem = item.Format.Value,
            };

            item.Format.SubscribeChanged(() => dropdown.SelectedItem = item.Format.Value, true);
            dropdown.Subscribe(ComboBox.SelectedItemProperty, c => item.Format.Value = (string) (c ?? item.FormatRenderers.Keys.First()));

            return dropdown;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
    class DataGridRendererDropdownColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var dropdown = new ComboBox()
            {
                Items = item.FormatRenderers[item.Format.Value],
                SelectedItem = item.Renderer.Value,
            };

            item.Format.SubscribeChanged(() =>
            {
                dropdown.Items = item.FormatRenderers[item.Format.Value];
                item.Renderer.Value = item.FormatRenderers[item.Format.Value][0];
            });

            item.Renderer.SubscribeChanged(() => dropdown.SelectedItem = item.Renderer.Value, true);
            dropdown.Subscribe(ComboBox.SelectedItemProperty, c => item.Renderer.Value = (string) (c ?? item.FormatRenderers[item.FormatRenderers.Keys.First()][0]));

            return dropdown;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
    class DataGridFormatVersionColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var tb = new TextBox() { Text = "1" };
            item.RendererVersion.SubscribeChanged(() => tb.Text = item.RendererVersion.Value, true);
            tb.Subscribe(TextBox.TextProperty, c => item.RendererVersion.Value = c);

            return tb;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
    class DataGridRendererVersionColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var tb = new TextBox();
            item.FormatVersion.SubscribeChanged(() => tb.Text = item.FormatVersion.Value, true);
            tb.Subscribe(TextBox.TextProperty, c => item.FormatVersion.Value = c);

            return tb;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
}
