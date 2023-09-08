using Avalonia.Controls.Utils;
using Avalonia.Data;
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

        var infos = request.Infos.Select(r => r.Renderers is null ? new ReadOnlyModelInfo(r) : new EditableModelInfo(NativeItem, r)).ToArray();

        var grid = new DataGrid() { AutoGenerateColumns = false, Items = infos, };
        grid.BeginningEdit += (obj, e) => e.Cancel = true;

        grid.Columns.Add(new DataGridTextColumn() { Binding = new Binding(nameof(ReadOnlyModelInfo.Name)), Header = "Name" });
        grid.Columns.Add(new DataGridTextColumn() { Binding = new Binding(nameof(ReadOnlyModelInfo.Format)), Header = "Format" });
        grid.Columns.Add(new DataGridCheckboxColumn() { Header = "Is native" });
        grid.Columns.Add(new DataGridRendererDropdownColumn() { Header = "Renderer" });
        grid.Columns.Add(new DataGridFormatVersionColumn() { Header = "Format version" });
        grid.Columns.Add(new DataGridRendererVersionColumn() { Header = "Renderer version" });

        Content = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* Auto"),
            Children =
            {
                grid.WithRow(0),
                new MPButton()
                {
                    Text = "OK",
                    OnClickSelf = async self => await click(self),
                }.WithRow(1),
            },
        };


        async Task click(MPButton self)
        {
            foreach (var item in infos.OfType<EditableModelInfo>())
            {
                if (NativeItem.Value is null)
                {
                    await self.FlashError("No native selected");
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
                if (item.FormatVersion.Value is not null && item.FormatVersion.Value.Length != 0 && !double.TryParse(item.FormatVersion.Value, out _))
                {
                    await self.FlashError("Invalid format version");
                    return;
                }
                if (item.RendererVersion.Value is not null && item.RendererVersion.Value.Length != 0 && !double.TryParse(item.RendererVersion.Value, out _))
                {
                    await self.FlashError("Invalid renderer version");
                    return;
                }
            }

            await onClick(JObject.FromObject(
                new InputTurboSquidModelInfoRequest.Response(
                    infos.Select(info =>
                        info is EditableModelInfo einfo
                        ? new InputTurboSquidModelInfoRequest.Response.ResponseModelInfo(
                            einfo.IsNative.Value, einfo.Renderer.Value,
                            double.Parse(einfo.FormatVersion.Value.ThrowIfNull(), CultureInfo.InvariantCulture), double.Parse(einfo.RendererVersion.Value.ThrowIfNull(), CultureInfo.InvariantCulture)
                        )
                        : null
                    )
                    .ToImmutableArray()
                )
            ));
            Dispatcher.UIThread.Post(ForceClose);
        }
    }


    class ReadOnlyModelInfo
    {
        public string Name { get; }
        public string Format { get; }

        public ReadOnlyModelInfo(InputTurboSquidModelInfoRequest.ModelInfo info)
        {
            Name = info.Name;
            Format = info.Format;
        }
    }
    class EditableModelInfo : ReadOnlyModelInfo
    {
        public ImmutableArray<string> Renderers { get; }

        public Bindable<bool> IsNative { get; }
        public Bindable<string> Renderer { get; }
        public Bindable<string> FormatVersion { get; }
        public Bindable<string?> RendererVersion { get; }

        public EditableModelInfo(Bindable<EditableModelInfo?> nativeItem, InputTurboSquidModelInfoRequest.ModelInfo info) : base(info)
        {
            Renderers = info.Renderers.ThrowIfValueNull();

            FormatVersion = new("1");
            RendererVersion = new();
            Renderer = new(Renderers[0]);

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
    class DataGridRendererDropdownColumn : DataGridColumn
    {
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if (dataItem is not EditableModelInfo item) return new Control();

            var dropdown = new ComboBox()
            {
                Items = item.Renderers,
                SelectedItem = item.Renderer.Value,
            };

            item.Renderer.SubscribeChanged(() => dropdown.SelectedItem = item.Renderer.Value, true);
            dropdown.Subscribe(ComboBox.SelectedItemProperty, c => item.Renderer.Value = (string) (c ?? item.Renderers[0]));

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

            var tb = new TextBox();
            item.FormatVersion.SubscribeChanged(() => tb.Text = item.FormatVersion.Value, true);
            tb.Subscribe(TextBox.TextProperty, c => item.FormatVersion.Value = c);

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
            item.RendererVersion.SubscribeChanged(() => tb.Text = item.RendererVersion.Value, true);
            tb.Subscribe(TextBox.TextProperty, c => item.RendererVersion.Value = c);

            return tb;
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
    }
}
