namespace Node.UI
{
    public static class LocaleBind
    {
        public static void Bind<TValue>(this AvaloniaObject obj, AvaloniaProperty<TValue> property, IReadOnlyBindable<TValue> text)
        {
            text = text.GetBoundCopy();

            // attached property to not let GC collect `text`
            var gcprop = new AttachedProperty<IReadOnlyBindable<TValue>>("SavedBindable", typeof(IReadOnlyBindable<TValue>), new StyledPropertyMetadata<IReadOnlyBindable<TValue>>());
            obj[gcprop] = text;

            text.SubscribeChanged(() => obj[property] = text.Value, true);
        }
        public static TObj Bind<TObj, TValue>(this TObj obj, AvaloniaProperty<TValue> property, IReadOnlyBindable<TValue> text) where TObj : AvaloniaObject
        {
            Bind((AvaloniaObject) obj, property, text);
            return obj;
        }

        public static TextBlock Bind(this TextBlock obj, IReadOnlyBindable<string?> text) => Bind(obj, TextBlock.TextProperty, text);
        public static Button Bind(this Button obj, IReadOnlyBindable<string?> text) => Bind(obj, Button.ContentProperty, text);

        public static void Bind(this AvaloniaObject obj, AvaloniaProperty property, LocalizedString text)
        {
            obj[property] = text.ToString();
            LocalizedString.ChangeLangWeakEvent.Subscribe(obj, () => Dispatcher.UIThread.Post(() => obj[property] = text.ToString()));
        }
        public static T Bind<T>(this T obj, AvaloniaProperty property, LocalizedString text) where T : AvaloniaObject
        {
            Bind((AvaloniaObject) obj, property, text);
            return obj;
        }
        public static TextBlock Bind(this TextBlock obj, LocalizedString text) => Bind(obj, TextBlock.TextProperty, text);
        public static Button Bind(this Button obj, LocalizedString text) => Bind(obj, Button.ContentProperty, text);
    }
}