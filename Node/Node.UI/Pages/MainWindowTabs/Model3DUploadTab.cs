using System.Net;
using _3DProductsPublish;
using _3DProductsPublish._3DProductDS;

namespace Node.UI.Pages.MainWindowTabs;

public class Model3DUploadTab : Panel
{
    public Model3DUploadTab()
    {
        var meta = JObject.FromObject(
            new _3DProduct.Metadata_()
            {
                Title = "CAT X-Æ 727",
                Description = "Dog on the catwalk",
                Tags = new[] { "cat", "dog", "car", "death" },
                License = _3DProduct.Metadata_.License_.Editorial,
                Polygons = 123456,
                Vertices = 321654,
                Price = 727,
            },
            JsonSettings.LowercaseS
        );

        var cgtradercredsinput = new CredentialsInput();
        var turbosquidcredsinput = new CredentialsInput();
        var dirpicker = new LocalDirSetting();
        var submitbtn = new MPButton()
        {
            Text = "Submit",
            Margin = new Thickness(0, 0, 0, 5),
            OnClickSelf = async self =>
            {
                var cgcreds = cgtradercredsinput.TryGet();
                var turbocreds = turbosquidcredsinput.TryGet();
                if (cgcreds is null || turbocreds is null)
                    return;

                if (string.IsNullOrEmpty(dirpicker.Dir))
                {
                    await self.FlashError("No directory selected");
                    return;
                }
                if (!Directory.Exists(dirpicker.Dir))
                {
                    await self.FlashError("Directory doesn't exists");
                    return;
                }


                ProcessObject(meta);
                ProcessObject(meta); // two times, yes

                var cred = new _3DProductPublisher.Credentials(cgcreds, turbocreds);

                var result = await LocalApi.Default.Post("3dupload", "Uploading 3d item",
                    ("creds", JsonConvert.SerializeObject(cred)), ("meta", meta.ToString()), ("dir", dirpicker.Dir)
                );
                await self.FlashErrorIfErr(result);
            },
        };

        Children.Add(new ScrollViewer()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto Auto Auto *"),
                Children =
                {
                    cgtradercredsinput.Named("CGTrader credentials").WithRow(0),
                    turbosquidcredsinput.Named("Turbosquid credentials").WithRow(1),
                    submitbtn.Named("Submit").WithRow(2),
                    dirpicker.Named("Model directory").WithRow(3),
                    JsonUISetting.Create(new JProperty("__", meta), FieldDescriber.Create(typeof(_3DProduct.Metadata_)))
                        .Named("Metadata").WithRow(4),
                },
            },
        });
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
        public string? Dir { get; private set; }

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
    class CredentialsInput : Panel
    {
        readonly TextBox Username, Password;

        public CredentialsInput()
        {
            Username = new() { Watermark = "Username" };
            Password = new() { Watermark = "Password" };

            Children.Add(new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto"),
                Children =
                {
                    Username.WithRow(0),
                    Password.WithRow(1),
                },
            });
        }

        public NetworkCredential? TryGet()
        {
            Username.BorderBrush = Password.BorderBrush = null;

            if (string.IsNullOrEmpty(Username.Text))
                Username.BorderBrush = Brushes.Red;
            if (string.IsNullOrEmpty(Password.Text))
                Password.BorderBrush = Brushes.Red;

            if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Text))
                return null;

            return new NetworkCredential(Username.Text, Password.Text);
        }
    }
}
