using Avalonia.Controls.Templates;

namespace Node.UI.Controls;

public static class TypedItemsControl
{
    public static TypedItemsControl<T> Create<T>(IReadOnlyBindable<IReadOnlyCollection<T>> items, Func<T, Control> func) => new(items, func);
}
public class TypedItemsControl<T> : ItemsControl
{
    protected override Type StyleKeyOverride => typeof(ItemsControl);

    // GC protected instance
    readonly object GCItems;

    public TypedItemsControl(IReadOnlyBindable<IReadOnlyCollection<T>> items, Func<T, Control> func)
    {
        GCItems = items = items.GetBoundCopy();
        items.SubscribeChanged(() => Dispatcher.UIThread.Post(() => ItemsSource = items.Value), true);

        ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
    }
}

