using Avalonia.Controls.Templates;

namespace Node.UI.Controls;

public static class TypedListBox
{
    public static TypedListBox<T> Create<T>(IReadOnlyCollection<T> items, Func<T, IControl> func) => new TypedListBox<T>(items, func);

    public static TypedListBox<T> CreateBinded<T>(IReadOnlyBindableCollection<T> items, Func<T, IControl> func)
    {
        items = items.GetBoundCopy();

        var list = new TypedListBox<T>(items, func);
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => list.Items = items), true);

        return list;
    }
}
public class TypedListBox<T> : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);
    public new T SelectedItem => (T) base.SelectedItem!;

    public TypedListBox(IReadOnlyCollection<T> items, Func<T, IControl> func)
    {
        Items = items;
        ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

