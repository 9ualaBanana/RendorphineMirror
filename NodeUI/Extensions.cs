namespace NodeUI
{
    public static class Extensions
    {
        public static T With<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }

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
    }
}