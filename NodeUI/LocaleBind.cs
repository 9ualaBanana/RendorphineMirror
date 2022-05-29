namespace NodeUI
{
    public static class LocaleBind
    {
        public static void Bind(this AvaloniaObject obj, AvaloniaProperty property, LocalizedString text)
        {
            obj[property] = text.ToString();
            LocalizedString.ChangeLangWeakEvent.Subscribe(obj, () => obj[property] = text.ToString());
        }
        public static T Binded<T>(this T obj, AvaloniaProperty property, LocalizedString text) where T : AvaloniaObject
        {
            Bind(obj, property, text);
            return obj;
        }
        public static TextBlock Binded(this TextBlock obj, LocalizedString text) => Binded(obj, TextBlock.TextProperty, text);
    }
}