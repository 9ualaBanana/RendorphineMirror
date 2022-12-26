using System.Web;
using _3DProductsPublish.CGTrader._3DModelComponents;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages.MainWindowTabs;

public class CGTraderUploadTab : Panel
{
    public CGTraderUploadTab()
    {
        var dirpicker = new LocalDirSetting();
        var username = new TextBox() { Text = "username" };
        var password = new TextBox() { Text = "password" };
        var meta = JObject.FromObject(
            CGTrader3DProductMetadata.ForCG(
                "titel",
                "decrepten",
                new[] { "tag1", "teg2", "tag3", "tag4", "tag5" },
                CGTrader3DProductCategory.Car(CarSubCategory.SUV),
                NonCustomCGTraderLicense.royalty_free
            ), JsonSettings.LowercaseS);

        var grid = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("Auto Auto Auto Auto Auto"),
            Children =
            {
                new MPButton()
                {
                    Text = "Upload",
                    Margin = new Thickness(0, 0, 0, 5),
                    OnClickSelf = async self =>
                    {
                        ProcessObject(meta);
                        ProcessObject(meta); // two times yes

                        var query = "uploadcgtrader";
                        query += "?username=" + HttpUtility.UrlEncode(username.Text);
                        query += "&password=" + HttpUtility.UrlEncode(password.Text);
                        query += "&directory=" + HttpUtility.UrlEncode(dirpicker.Dir);
                        query += "&meta=" + HttpUtility.UrlEncode(meta.ToString(Newtonsoft.Json.Formatting.None));

                        var result = await LocalApi.Send(query);
                        await self.FlashErrorIfErr(result);
                    },
                }.WithRow(0),
                dirpicker.WithRow(1),
                username.WithRow(2),
                password.WithRow(3),
                JsonUISetting.Create(new JProperty("__", meta), FieldDescriber.Create(typeof(CGTrader3DProductMetadata))).WithRow(4),
            },
        };

        Children.Add(new ScrollViewer() { Content = grid });
    }
    static void ForEachProperty(JToken jobj, Action<JObject, JProperty> func)
    {
        foreach (var (parent, property) in getProperties(jobj).ToArray())
            func(parent, property);

        IEnumerable<(JObject parent, JProperty property)> getProperties(JToken token) =>
            token is not JObject obj
                ? Enumerable.Empty<(JObject, JProperty)>()
                : obj.Properties().Select(p => (obj, p))
                    .Concat(obj.Values().SelectMany(getProperties));
    }
    static void ProcessObject(JToken jobj)
    {
        ForEachProperty(jobj, (parent, property) =>
        {
            // remove null
            if (property.Value is null || property.Value?.Type == JTokenType.Null)
                parent.Remove(property.Name);
        });
    }


    class LocalDirSetting : Panel
    {
        public string Dir { get; private set; } = null!;

        public LocalDirSetting()
        {
            var textinput = new TextBox();
            textinput.Subscribe(TextBox.TextProperty, text => Dir = text);

            var btn = new MPButton() { Text = new("Pick a directory") };
            btn.OnClick += () => new OpenFolderDialog().ShowAsync((Window) VisualRoot!).ContinueWith(t => Dispatcher.UIThread.Post(() => textinput.Text = new(t.Result ?? string.Empty)));

            var grid = new Grid()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("8* 2*"),
                Children = { textinput.WithColumn(0), btn.WithColumn(1), },
            };
            Children.Add(grid);
        }
    }
}
