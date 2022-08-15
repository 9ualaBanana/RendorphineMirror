using Avalonia.Controls.Templates;

namespace NodeUI.Controls;

public static class TypedItemsControl
{
    public static TypedItemsControl<T> Create<T>(IReadOnlyCollection<T> items, Func<T, IControl> func) => new TypedItemsControl<T>(items, func);

    public static TypedItemsControl<T> CreateBinded<T>(IReadOnlyBindableCollection<T> items, Func<T, IControl> func)
    {
        items = items.GetBoundCopy();

        var list = new TypedItemsControl<T>(items, func);
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => list.Items = items), true);

        return list;
    }
}
public class TypedItemsControl<T> : ItemsControl, IStyleable
{
    Type IStyleable.StyleKey => typeof(ItemsControl);

    public TypedItemsControl(IReadOnlyCollection<T> items, Func<T, IControl> func)
    {
        Items = items;
        ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

