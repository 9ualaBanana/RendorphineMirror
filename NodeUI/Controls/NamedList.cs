namespace NodeUI.Controls;

public static class NamedList
{
    public static NamedList<T> Create<T>(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) => new(title, items, templatefunc);
}
public class NamedControl : Panel
{
    protected readonly string TitleText;
    protected readonly TextBlock Title;
    public readonly Panel Control;

    public NamedControl(string title)
    {
        TitleText = title;
        Title = new TextBlock();
        Control = new Panel();

        UpdateTitle();


        Children.Add(new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("Auto *"),
            Children =
            {
                Title.WithRow(0),
                Control.WithRow(1),
            },
        });
    }

    public void UpdateTitle() => Dispatcher.UIThread.Post(() => Title.Text = $"{TitleText}\nLast update: {DateTimeOffset.Now}");
}
public class NamedList<T> : NamedControl
{
    // GC protected instance
    readonly IReadOnlyCollection<T> Items;

    public NamedList(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) : base(title)
    {
        Items = items = (items as IReadOnlyBindableCollection<T>)?.GetBoundCopy() ?? items;

        (items as IReadOnlyBindableCollection<T>)?.SubscribeChanged(UpdateTitle, true);
        Control.Children.Add(TypedItemsControl.Create(items, templatefunc));
    }
}
