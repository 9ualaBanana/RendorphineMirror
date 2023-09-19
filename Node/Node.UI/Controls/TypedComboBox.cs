using System.Reflection;
using Avalonia.Controls.Templates;

namespace Node.UI.Controls;

public static class TypedComboBox
{
    public static TypedComboBox<T> Create<T>(IReadOnlyCollection<T> items, Func<T, IControl>? func = null) => new TypedComboBox<T>(items, func);

    public static TypedComboBox<T> CreateBinded<T>(IReadOnlyBindableCollection<T> items, Func<T, IControl>? func = null)
    {
        items = items.GetBoundCopy();

        var list = new TypedComboBox<T>(items, func);
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => list.Items = items), true);

        return list;
    }
}
public class TypedComboBox<T> : ComboBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ComboBox);

    public new IReadOnlyCollection<T> Items
    {
        get => (IReadOnlyCollection<T>) base.Items;
        set
        {
            var selected = SelectedItem;
            typeof(ItemsControl).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).ThrowIfNull()
                .SetValue(this, value);

            //base.Items = value;
            SelectedItem = selected;
        }
    }

    public new T SelectedItem
    {
        get => (T) (base.SelectedItem ??= base.Items.OfType<T>().FirstOrDefault())!;
        set
        {
            if (base.Items.OfType<T>().Contains(value))
                base.SelectedItem = value;
            else SelectedIndex = 0;
        }
    }

    public TypedComboBox(IReadOnlyCollection<T> items, Func<T, IControl>? func = null)
    {
        Items = items;
        if (func is not null) ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

