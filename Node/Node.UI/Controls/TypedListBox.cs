using Avalonia.Controls.Templates;

namespace Node.UI.Controls;

public static class TypedListBox
{
    public static TypedListBox<T> Create<T>(IReadOnlyCollection<T> items, Func<T, Control> func) => new TypedListBox<T>(items, func);

    public static TypedListBox<T> CreateBinded<T>(IReadOnlyBindableCollection<T> items, Func<T, Control> func)
    {
        items = items.GetBoundCopy();

        var list = new TypedListBox<T>(items, func);
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => list.ItemsSource = items), true);

        return list;
    }
}
public class TypedListBox<T> : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);
    public new T SelectedItem => (T) base.SelectedItem!;

    public TypedListBox(IReadOnlyCollection<T> items, Func<T, Control> func)
    {
        ItemsSource = items;
        ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

