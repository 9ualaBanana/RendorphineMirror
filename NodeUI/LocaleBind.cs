namespace NodeUI
{
    public static class LocaleBind
    {
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