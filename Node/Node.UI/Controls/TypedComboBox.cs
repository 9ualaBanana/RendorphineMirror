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

    public new IEnumerable<T> Items { get => (IEnumerable<T>) base.Items; set => base.Items = value; }

    public new T SelectedItem =>
        typeof(T).IsEnum
        ? (T) Enum.Parse(typeof(T), base.SelectedItem!.ToString()!)
        : (T) base.SelectedItem!;

    public TypedComboBox(IReadOnlyCollection<T> items, Func<T, IControl>? func = null)
    {
        Items = items;
        if (func is not null) ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

