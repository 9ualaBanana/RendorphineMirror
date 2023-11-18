namespace Node.UI.Controls;

public static class NamedList
{
    public static NamedList<T> CreateRaw<T>(LocalizedString title, IReadOnlyCollection<T> items, Func<T, Control> templatefunc) => Create(title, new Bindable<IReadOnlyCollection<T>>(items), templatefunc);
    public static NamedList<T> Create<T>(LocalizedString title, IReadOnlyBindable<IReadOnlyCollection<T>> items, Func<T, Control> templatefunc) => new(title, items, templatefunc);
}
public class NamedList<T> : NamedControl
{
    // GC protected instance
    readonly object GCItems;

    public NamedList(LocalizedString title, IReadOnlyBindable<IReadOnlyCollection<T>> items, Func<T, Control> templatefunc) : base(title)
    {
        GCItems = items = items.GetBoundCopy();

        (items as IReadOnlyBindableCollection<T>)?.SubscribeChanged(() => Dispatcher.UIThread.Post(() => Title.Text = $"{title}\nLast update: {DateTime.Now}"), true);
        Control.Children.Add(TypedItemsControl.Create(items, templatefunc));
    }
}
