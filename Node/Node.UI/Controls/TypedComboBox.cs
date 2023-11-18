using System.Reflection;
using Avalonia.Controls.Templates;

namespace Node.UI.Controls;

public static class TypedComboBox
{
    public static TypedComboBox<T> Create<T>(IReadOnlyCollection<T> items, Func<T, Control>? func = null) => new TypedComboBox<T>(items, func);

    public static TypedComboBox<T> CreateBinded<T>(IReadOnlyBindableCollection<T> items, Func<T, Control>? func = null)
    {
        items = items.GetBoundCopy();

        var list = new TypedComboBox<T>(items, func);
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => list.Items = items), true);

        return list;
    }
}
public class TypedComboBox<T> : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);
    public new IEnumerable<T> Items { get => (IEnumerable<T>) base.Items; set => base.ItemsSource = value; }

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

    public TypedComboBox(IReadOnlyCollection<T> items, Func<T, Control>? func = null)
    {
        Items = items;
        if (func is not null) ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

