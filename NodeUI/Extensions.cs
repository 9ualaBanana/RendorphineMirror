namespace NodeUI
{
    public static class Extensions
    {
        public static T WithRow<T>(this T obj, int value) where T : Control => obj.With(x => Grid.SetRow(x, value));
        public static T WithColumn<T>(this T obj, int value) where T : Control => obj.With(x => Grid.SetColumn(x, value));
        public static T WithRowSpan<T>(this T obj, int value) where T : Control => obj.With(x => Grid.SetRowSpan(x, value));
        public static T WithColumnSpan<T>(this T obj, int value) where T : Control => obj.With(x => Grid.SetColumnSpan(x, value));

        public static T WithRowColumn<T>(this T obj, int row, int column) where T : Control => obj.WithRow(row).WithColumn(column);

        public static TObj Subscribe<TObj, TValue>(this TObj obj, AvaloniaProperty<TValue> property, Action<TValue> changed) where TObj : Control
        {
            obj.GetObservable<TValue>(property).Subscribe(changed);
            return obj;
        }


        public static T Centered<T>(this T obj) where T : Control => obj.With(x => { x.HorizontalAlignment = HorizontalAlignment.Center; x.VerticalAlignment = VerticalAlignment.Center; });

        public static T Bound<T, TVal>(this T obj, AvaloniaProperty<TVal> property, IBindable<TVal> bindable) where T : Control
        {
            bindable = bindable.GetBoundCopy();

            bindable.SubscribeChanged(() => obj.SetValue(property, bindable.Value), true);
            obj.Subscribe(property, v => bindable.Value = v);

            return obj;
        }
    }
}