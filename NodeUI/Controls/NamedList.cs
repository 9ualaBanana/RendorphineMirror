namespace NodeUI.Controls;

public static class NamedList
{
    public static NamedList<T> Create<T>(LocalizedString title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) => new(title, items, templatefunc);
}
public class NamedList<T> : NamedControl
{
    // GC protected instance
    readonly IReadOnlyCollection<T> Items;

    public NamedList(LocalizedString title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) : base(title)
    {
        Items = items = (items as IReadOnlyBindableCollection<T>)?.GetBoundCopy() ?? items;

        (items as IReadOnlyBindableCollection<T>)?.SubscribeChanged(() => Dispatcher.UIThread.Post(() => Title.Text = $"{title}\nLast update: {DateTime.Now}"), true);
        Control.Children.Add(TypedItemsControl.Create(items, templatefunc));
    }
}
